//OriginalPoster.cs
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OriginalPoster.Reflection.EmbyLocalMetadata;
using static OriginalPoster.Reflection.EmbyProviders;
using static OriginalPoster.Reflection.MovieDb;
using static OriginalPoster.Reflection.Tvdb;

using Season = MediaBrowser.Controller.Entities.TV.Season;

// 注意：请将命名空间替换为您项目的实际名称
namespace OriginalPoster
{
    public class PreferOriginalPoster : PatchBase<PreferOriginalPoster>
    {
        internal class ContextItem
        {
            public string TmdbId { get; set; }
            public string ImdbId { get; set; }
            public string TvdbId { get; set; }
            public string OriginalLanguage { get; set; }
        }

        private static readonly ConcurrentDictionary<string, ContextItem> CurrentItemsByTmdbId =
            new ConcurrentDictionary<string, ContextItem>();
        private static readonly ConcurrentDictionary<string, ContextItem> CurrentItemsByImdbId =
            new ConcurrentDictionary<string, ContextItem>();
        private static readonly ConcurrentDictionary<string, ContextItem> CurrentItemsByTvdbId =
            new ConcurrentDictionary<string, ContextItem>();
        private static readonly ConcurrentDictionary<string, string> BackdropByLanguage =
            new ConcurrentDictionary<string, string>();

        private static readonly AsyncLocal<ContextItem> CurrentLookupItem = new AsyncLocal<ContextItem>();

        public PreferOriginalPoster()
        {
            Initialize();

            // --- 关键修改：使用新插件的独立配置 ---
            if (Plugin.Instance.Configuration.EnablePreferOriginalPoster)
            {
                Patch();
            }
            // --- 修改结束 ---
        }

        protected override void OnInitialize()
        {
            if (Reflection.MovieDb.IsSupported || Reflection.Tvdb.IsSupported)
            {
                ReversePatch(PatchTracker, _addLocalImage, nameof(AddLocalImageStub));
                ReversePatch(PatchTracker, _getLocalFiles, nameof(GetLocalFilesStub));
            }
            else
            {
                PatchTracker.FallbackPatchApproach = PatchApproach.None;
                PatchTracker.IsSupported = false;
            }
        }

        protected override void Prepare(bool apply)
        {
            if (Reflection.MovieDb.IsSupported)
            {
                PatchUnpatch(PatchTracker, apply, _getMovieInfo, postfix: nameof(GetMovieInfoTmdbPostfix));
                PatchUnpatch(PatchTracker, apply, _ensureSeriesInfo, postfix: nameof(EnsureSeriesInfoTmdbPostfix));
                PatchUnpatch(PatchTracker, apply, _getBackdrops, postfix: nameof(GetBackdropsPostfix));
            }

            if (Reflection.Tvdb.IsSupported)
            {
                PatchUnpatch(PatchTracker, apply, _ensureMovieInfoTvdb, postfix: nameof(EnsureMovieInfoTvdbPostfix));
                PatchUnpatch(PatchTracker, apply, _ensureSeriesInfoTvdb, postfix: nameof(EnsureSeriesInfoTvdbPostfix));
            }

            PatchUnpatch(PatchTracker, apply, _getAvailableRemoteImages,
                prefix: nameof(GetAvailableRemoteImagesPrefix), postfix: nameof(GetAvailableRemoteImagesPostfix));
            PatchUnpatch(PatchTracker, apply, _populateSeasonImagesFromSeasonOrSeriesFolder,
                postfix: nameof(PopulateSeasonImagesFromSeasonOrSeriesFolderPostfix));
        }

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
            if (lookupItem.TmdbId != null)
            {
                CurrentItemsByTmdbId.TryRemove(lookupItem.TmdbId, out foundItem);
            }
            if (foundItem == null && lookupItem.ImdbId != null)
            {
                CurrentItemsByImdbId.TryRemove(lookupItem.ImdbId, out foundItem);
            }
            if (foundItem == null && lookupItem.TvdbId != null)
            {
                CurrentItemsByTvdbId.TryRemove(lookupItem.TvdbId, out foundItem);
            }

            return foundItem;
        }

        private static string GetOriginalLanguage(BaseItem item)
        {
            var itemLookup = GetAndRemoveItem();
            if (itemLookup != null && !string.IsNullOrEmpty(itemLookup.OriginalLanguage))
                return itemLookup.OriginalLanguage;

            var fallbackItem = item switch
            {
                Movie or Series => item,
                Season season => season.Series,
                Episode episode => episode.Series,
                _ => null
            };

            if (fallbackItem != null)
            {
                return LanguageUtility.GetLanguageByTitle(fallbackItem.OriginalTitle);
            }

            if (item is BoxSet collection)
            {
                return Plugin.MetadataApi.GetCollectionOriginalLanguage(collection);
            }

            return null;
        }

        [HarmonyPostfix]
        private static void GetMovieInfoTmdbPostfix(BaseItem item, string language, IJsonSerializer jsonSerializer,
            CancellationToken cancellationToken, Task __result)
        {
            CompleteMovieData movieData = null;
            try
            {
                movieData = Traverse.Create(__result).Property("Result").GetValue<CompleteMovieData>();
            }
            catch
            {
                // ignored
            }

            if (movieData != null)
            {
                var tmdbId = Traverse.Create(movieData).Property("id").GetValue<int>().ToString();
                var imdbId = Traverse.Create(movieData).Property("imdb_id").GetValue<string>();
                var originalLanguage = Traverse.Create(movieData).Property("original_language").GetValue<string>();

                if ((!string.IsNullOrEmpty(tmdbId) || !string.IsNullOrEmpty(imdbId)) &&
                    !string.IsNullOrEmpty(originalLanguage))
                {
                    UpdateOriginalLanguage(tmdbId, imdbId, null, originalLanguage);
                }
            }
        }

        [HarmonyPostfix]
        private static void EnsureSeriesInfoTmdbPostfix(string tmdbId, string language, Task __result)
        {
            if (!WasCalledByMethod(_movieDbAssembly, "FetchImages")) return;

            SeriesRootObject seriesInfo = null;
            try
            {
                seriesInfo = Traverse.Create(__result).Property("Result").GetValue<SeriesRootObject>();
            }
            catch
            {
                // ignored
            }

            if (seriesInfo != null)
            {
                var id = seriesInfo.id.ToString();
                var originalLanguage = seriesInfo.languages?.FirstOrDefault();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(originalLanguage))
                {
                    UpdateOriginalLanguage(id, null, null, originalLanguage);
                }
            }
        }

        [HarmonyPostfix]
        private static void EnsureMovieInfoTvdbPostfix(string tvdbId, Task __result)
        {
            if (!WasCalledByMethod(_tvdbAssembly, "GetImages")) return;

            MovieData movieData = null;
            try
            {
                movieData = Traverse.Create(__result).Property("Result").GetValue<MovieData>();
            }
            catch
            {
                // ignored
            }

            if (movieData != null)
            {
                var id = movieData.id.ToString();
                var originalLanguage = movieData.originalLanguage;

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(originalLanguage))
                {
                    var convertedLanguage = Plugin.MetadataApi.ConvertToServerLanguage(originalLanguage);
                    UpdateOriginalLanguage(null, null, id, convertedLanguage);
                }
            }
        }

        [HarmonyPostfix]
        private static void EnsureSeriesInfoTvdbPostfix(string tvdbId, Task __result)
        {
            if (!WasCalledByMethod(_tvdbAssembly, "GetImages")) return;

            SeriesData seriesData = null;
            try
            {
                seriesData = Traverse.Create(__result).Property("Result").GetValue<SeriesData>();
            }
            catch
            {
                // ignored
            }

            if (seriesData != null)
            {
                var id = seriesData.id.ToString();
                var originalLanguage = seriesData.originalLanguage;

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(originalLanguage))
                {
                    var convertedLanguage = Plugin.MetadataApi.ConvertToServerLanguage(originalLanguage);
                    UpdateOriginalLanguage(null, null, id, convertedLanguage);
                }
            }
        }

        [HarmonyPostfix]
        private static void GetBackdropsPostfix(IEnumerable<object> __result)
        {
            if (__result is IEnumerable<TmdbImage> images)
            {
                foreach (var image in images)
                {
                    var filePath = image.file_path;
                    var language = image.iso_639_1;

                    if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(language))
                    {
                        BackdropByLanguage[filePath] = language;
                        image.iso_639_1 = null;
                    }
                }
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
            IEnumerable<RemoteImageInfo> result = null;
            try
            {
                result = __result?.Result;
            }
            catch
            {
                // ignored
            }

            if (result is null) return Task.FromResult(Enumerable.Empty<RemoteImageInfo>());

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

        [HarmonyReversePatch]
        private static FileSystemMetadata[] GetLocalFilesStub(ILocalImageFileProvider instance, BaseItem item,
            LibraryOptions libraryOptions, bool includeDirectories, IDirectoryService directoryService) =>
            throw new NotImplementedException();

        [HarmonyReversePatch]
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
                var name = nameof(season) + indexNumber.Value.ToString("00", CultureInfo.InvariantCulture) + "-poster";

                var seriesFolderFiles =
                    GetLocalFilesStub(__instance, season.Series, libraryOptions, false, directoryService);
                var result = AddLocalImageStub(__instance, seriesFolderFiles, images, name, ImageType.Primary);

                if (result) return;

                var seasonFolderFiles = GetLocalFilesStub(__instance, season, libraryOptions, false, directoryService);
                AddLocalImageStub(__instance, seasonFolderFiles, images, name, ImageType.Primary);
            }
        }
    }
}
