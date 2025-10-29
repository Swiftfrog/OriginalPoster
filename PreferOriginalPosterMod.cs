using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace PreferOriginalPoster.Plugin
{
    public static class PreferOriginalPosterMod
    {
        private static Harmony _harmony;
        private static ILogger _logger;

        // 静态存储（线程安全）
        private static readonly ConcurrentDictionary<string, ContextItem> CurrentItemsByTmdbId = new();
        private static readonly ConcurrentDictionary<string, ContextItem> CurrentItemsByImdbId = new();
        private static readonly ConcurrentDictionary<string, ContextItem> CurrentItemsByTvdbId = new();
        private static readonly ConcurrentDictionary<string, string> BackdropByLanguage = new();
        private static readonly AsyncLocal<ContextItem> CurrentLookupItem = new();

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
            _harmony = new Harmony("com.swiftfrog.preferoriginalposter");

            var movieDbAssembly = GetAssembly("MovieDb");
            var tvdbAssembly = GetAssembly("Tvdb");
            var embyControllerAssembly = GetAssembly("MediaBrowser.Controller");

            bool patched = false;

            if (movieDbAssembly != null)
            {
                PatchMethod(movieDbAssembly, "MovieDbProvider", "GetMovieInfo", null, nameof(GetMovieInfoTmdbPostfix));
                PatchMethod(movieDbAssembly, "MovieDbSeriesProvider", "EnsureSeriesInfo", null, nameof(EnsureSeriesInfoTmdbPostfix));
                PatchMethod(movieDbAssembly, "MovieDbProvider", "GetBackdrops", null, nameof(GetBackdropsPostfix));
                patched = true;
            }

            if (tvdbAssembly != null)
            {
                PatchMethod(tvdbAssembly, "TvdbMovieProvider", "EnsureMovieInfo", null, nameof(EnsureMovieInfoTvdbPostfix));
                PatchMethod(tvdbAssembly, "TvdbSeriesProvider", "EnsureSeriesInfo", null, nameof(EnsureSeriesInfoTvdbPostfix));
                patched = true;
            }

            if (embyControllerAssembly != null)
            {
                PatchMethod(embyControllerAssembly, "MediaBrowser.Controller.Providers.ProviderManager", "GetAvailableRemoteImages",
                    nameof(GetAvailableRemoteImagesPrefix), nameof(GetAvailableRemoteImagesPostfix));
                PatchMethod(embyControllerAssembly, "Emby.LocalMetadata.Images.LocalImageProvider", "PopulateSeasonImagesFromSeasonOrSeriesFolder",
                    null, nameof(PopulateSeasonImagesFromSeasonOrSeriesFolderPostfix));
                patched = true;
            }

            if (patched)
            {
                _logger.Info("PreferOriginalPoster patches applied.");
            }
            else
            {
                _logger.Warn("No target assemblies found. Plugin will not function.");
            }
        }

        private static Assembly GetAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static void PatchMethod(Assembly assembly, string typeName, string methodName, string prefix, string postfix)
        {
            var type = assembly?.GetType(typeName);
            if (type == null) return;

            var method = AccessTools.Method(type, methodName);
            if (method == null) return;

            var prefixMethod = string.IsNullOrEmpty(prefix) ? null : AccessTools.Method(typeof(PreferOriginalPosterMod), prefix);
            var postfixMethod = string.IsNullOrEmpty(postfix) ? null : AccessTools.Method(typeof(PreferOriginalPosterMod), postfix);

            try
            {
                _harmony.Patch(method, new HarmonyMethod(prefixMethod), new HarmonyMethod(postfixMethod));
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to patch {typeName}.{methodName}: {ex.Message}");
            }
        }

        // =============== Harmony 方法 ===============
        private static void AddContextItem(string tmdbId, string imdbId, string tvdbId)
        {
            if (tmdbId == null && imdbId == null && tvdbId == null) return;
            var item = new ContextItem { TmdbId = tmdbId, ImdbId = imdbId, TvdbId = tvdbId };
            if (tmdbId != null) CurrentItemsByTmdbId[tmdbId] = item;
            if (imdbId != null) CurrentItemsByImdbId[imdbId] = item;
            if (tvdbId != null) CurrentItemsByTvdbId[tvdbId] = item;
            CurrentLookupItem.Value = new ContextItem { TmdbId = tmdbId, ImdbId = imdbId, TvdbId = tvdbId };
        }

        private static void UpdateOriginalLanguage(string tmdbId, string imdbId, string tvdbId, string originalLanguage)
        {
            ContextItem itemToUpdate = null;
            if (tmdbId != null) CurrentItemsByTmdbId.TryGetValue(tmdbId, out itemToUpdate);
            if (itemToUpdate == null && imdbId != null) CurrentItemsByImdbId.TryGetValue(imdbId, out itemToUpdate);
            if (itemToUpdate == null && tvdbId != null) CurrentItemsByTvdbId.TryGetValue(tvdbId, out itemToUpdate);
            if (itemToUpdate != null) itemToUpdate.OriginalLanguage = originalLanguage;
        }

        private static ContextItem GetAndRemoveItem()
        {
            var lookupItem = CurrentLookupItem.Value;
            CurrentLookupItem.Value = null;
            if (lookupItem == null) return null;
            ContextItem foundItem = null;
            if (lookupItem.TmdbId != null) CurrentItemsByTmdbId.TryRemove(lookupItem.TmdbId, out foundItem);
            if (foundItem == null && lookupItem.ImdbId != null) CurrentItemsByImdbId.TryRemove(lookupItem.ImdbId, out foundItem);
            if (foundItem == null && lookupItem.TvdbId != null) CurrentItemsByTvdbId.TryRemove(lookupItem.TvdbId, out foundItem);
            return foundItem;
        }

        private static string GetOriginalLanguage(BaseItem item)
        {
            var itemLookup = GetAndRemoveItem();
            if (itemLookup != null && !string.IsNullOrEmpty(itemLookup.OriginalLanguage))
                return itemLookup.OriginalLanguage;

            // 简化：尝试从 OriginalTitle 推断（实际可留空或使用简单规则）
            var fallbackItem = item switch
            {
                Movie or Series => item,
                Season s => s.Series,
                Episode e => e.Series,
                _ => null
            };

            if (fallbackItem?.OriginalTitle != null)
            {
                // 简单语言检测（可扩展）
                if (fallbackItem.OriginalTitle.Any(c => c >= 0x0400 && c <= 0x04FF)) return "ru";
                if (fallbackItem.OriginalTitle.Any(c => c >= 0x4E00 && c <= 0x9FFF)) return "zh";
                if (fallbackItem.OriginalTitle.Any(c => c >= 0x3040 && c <= 0x309F) || 
                    fallbackItem.OriginalTitle.Any(c => c >= 0x30A0 && c <= 0x30FF)) return "ja";
            }

            return null;
        }

        [HarmonyPostfix]
        private static void GetMovieInfoTmdbPostfix(BaseItem item, string language, IJsonSerializer jsonSerializer,
            CancellationToken cancellationToken, Task __result)
        {
            try
            {
                var movieData = Traverse.Create(__result).Property("Result").GetValue<object>();
                if (movieData == null) return;

                var tmdbId = Traverse.Create(movieData).Property("id").GetValue<int>().ToString();
                var imdbId = Traverse.Create(movieData).Property("imdb_id").GetValue<string>();
                var originalLanguage = Traverse.Create(movieData).Property("original_language").GetValue<string>();

                if ((!string.IsNullOrEmpty(tmdbId) || !string.IsNullOrEmpty(imdbId)) &&
                    !string.IsNullOrEmpty(originalLanguage))
                {
                    UpdateOriginalLanguage(tmdbId, imdbId, null, originalLanguage);
                }
            }
            catch (Exception ex)
            {
                _logger?.Debug($"GetMovieInfoTmdbPostfix error: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void EnsureSeriesInfoTmdbPostfix(string tmdbId, string language, Task __result)
        {
            try
            {
                var seriesInfo = Traverse.Create(__result).Property("Result").GetValue<object>();
                if (seriesInfo == null) return;

                var id = Traverse.Create(seriesInfo).Property("id").GetValue<int>().ToString();
                var languages = Traverse.Create(seriesInfo).Field("languages").GetValue<string[]>();
                var originalLanguage = languages?.FirstOrDefault();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(originalLanguage))
                {
                    UpdateOriginalLanguage(id, null, null, originalLanguage);
                }
            }
            catch (Exception ex)
            {
                _logger?.Debug($"EnsureSeriesInfoTmdbPostfix error: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void EnsureMovieInfoTvdbPostfix(string tvdbId, Task __result)
        {
            try
            {
                var movieData = Traverse.Create(__result).Property("Result").GetValue<object>();
                if (movieData == null) return;

                var id = Traverse.Create(movieData).Property("id").GetValue<int>().ToString();
                var originalLanguage = Traverse.Create(movieData).Property("originalLanguage").GetValue<string>();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(originalLanguage))
                {
                    UpdateOriginalLanguage(null, null, id, originalLanguage);
                }
            }
            catch (Exception ex)
            {
                _logger?.Debug($"EnsureMovieInfoTvdbPostfix error: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void EnsureSeriesInfoTvdbPostfix(string tvdbId, Task __result)
        {
            try
            {
                var seriesData = Traverse.Create(__result).Property("Result").GetValue<object>();
                if (seriesData == null) return;

                var id = Traverse.Create(seriesData).Property("id").GetValue<int>().ToString();
                var originalLanguage = Traverse.Create(seriesData).Property("originalLanguage").GetValue<string>();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(originalLanguage))
                {
                    UpdateOriginalLanguage(null, null, id, originalLanguage);
                }
            }
            catch (Exception ex)
            {
                _logger?.Debug($"EnsureSeriesInfoTvdbPostfix error: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void GetBackdropsPostfix(IEnumerable<object> __result)
        {
            try
            {
                if (__result is not IEnumerable<object> images) return;
                foreach (var image in images)
                {
                    var filePath = Traverse.Create(image).Field("file_path").GetValue<string>();
                    var language = Traverse.Create(image).Field("iso_639_1").GetValue<string>();
                    if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(language))
                    {
                        BackdropByLanguage[filePath] = language;
                        Traverse.Create(image).Field("iso_639_1").SetValue(null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Debug($"GetBackdropsPostfix error: {ex.Message}");
            }
        }

        [HarmonyPrefix]
        private static bool GetAvailableRemoteImagesPrefix(IHasProviderIds item, LibraryOptions libraryOptions,
            ref RemoteImageQuery query, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            query.IncludeAllLanguages = true;
            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);
            var tvdbId = item.GetProviderId(MetadataProviders.Tvdb);
            AddContextItem(tmdbId, imdbId, tvdbId);
            return true;
        }

        [HarmonyPostfix]
        private static Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImagesPostfix(
            Task<IEnumerable<RemoteImageInfo>> __result, BaseItem item, LibraryOptions libraryOptions,
            RemoteImageQuery query, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            try
            {
                var result = __result?.Result;
                if (result == null) return Task.FromResult(Enumerable.Empty<RemoteImageInfo>());

                var originalLanguage = GetOriginalLanguage(item);
                var libraryPreferredImageLanguage = libraryOptions.PreferredImageLanguage?.Split('-')[0];
                var remoteImages = result.ToList();

                if (BackdropByLanguage.Count > 0)
                {
                    foreach (var image in remoteImages.Where(i => i.Type == ImageType.Backdrop))
                    {
                        foreach (var kvp in BackdropByLanguage)
                        {
                            if (image.Url.EndsWith(kvp.Key, StringComparison.Ordinal))
                            {
                                image.Language = kvp.Value;
                                BackdropByLanguage.TryRemove(kvp.Key, out _);
                                break;
                            }
                        }
                    }
                }

                var reorderedImages = remoteImages.OrderBy(i =>
                    (i.Type == ImageType.Backdrop || item is Episode) && string.IsNullOrEmpty(i.Language) ? 0 :
                    !string.IsNullOrEmpty(libraryPreferredImageLanguage) && string.Equals(i.Language,
                        libraryPreferredImageLanguage, StringComparison.OrdinalIgnoreCase) ? 0 :
                    !string.IsNullOrEmpty(originalLanguage) &&
                    string.Equals(i.Language, originalLanguage, StringComparison.OrdinalIgnoreCase) ? 1 : 2);

                return Task.FromResult(reorderedImages.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetAvailableRemoteImagesPostfix error: {ex.Message}");
                return Task.FromResult(__result?.Result ?? Enumerable.Empty<RemoteImageInfo>());
            }
        }

        // ReversePatch 方法（用于 Season 0）
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(LocalImageProvider), "GetFiles")]
        private static FileSystemMetadata[] GetLocalFilesStub(ILocalImageFileProvider instance, BaseItem item,
            LibraryOptions libraryOptions, bool includeDirectories, IDirectoryService directoryService) =>
            throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(LocalImageProvider), "AddImage")]
        private static bool AddLocalImageStub(ILocalImageFileProvider instance, FileSystemMetadata[] files,
            List<LocalImageInfo> images, string name, ImageType type) =>
            throw new NotImplementedException();

        [HarmonyPostfix]
        private static void PopulateSeasonImagesFromSeasonOrSeriesFolderPostfix(ILocalImageFileProvider __instance,
            Season season, LibraryOptions libraryOptions, List<LocalImageInfo> images,
            IDirectoryService directoryService)
        {
            var indexNumber = season.IndexNumber;
            if (indexNumber.HasValue && indexNumber.Value == 0 && images.All(i => i.Type != ImageType.Primary))
            {
                var name = "season" + indexNumber.Value.ToString("00", CultureInfo.InvariantCulture) + "-poster";
                var seriesFolderFiles = GetLocalFilesStub(__instance, season.Series, libraryOptions, false, directoryService);
                var result = AddLocalImageStub(__instance, seriesFolderFiles, images, name, ImageType.Primary);
                if (!result)
                {
                    var seasonFolderFiles = GetLocalFilesStub(__instance, season, libraryOptions, false, directoryService);
                    AddLocalImageStub(__instance, seasonFolderFiles, images, name, ImageType.Primary);
                }
            }
        }

        // =============== 嵌套类 ===============
        internal class ContextItem
        {
            public string TmdbId { get; set; }
            public string ImdbId { get; set; }
            public string TvdbId { get; set; }
            public string OriginalLanguage { get; set; }
        }
    }
}
