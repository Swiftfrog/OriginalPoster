using MediaBrowser.Controller.Entities; // BaseItem
using MediaBrowser.Controller.Entities.Movies; // For Movie type check
using MediaBrowser.Controller.Providers; // IRemoteImageProvider, IHasOrder
using MediaBrowser.Model.Configuration; // LibraryOptions
using MediaBrowser.Model.Entities; // ImageType
using MediaBrowser.Model.Logging; // ILogger
using MediaBrowser.Model.Providers; // RemoteImageInfo
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net; // For IHttpClient, HttpRequestOptions, HttpResponseInfo
using System.Text.Json; // For JSON parsing
using System.IO; // For Stream

namespace OriginalPoster.Providers
{
    // --- 移除 IHasOrder 接口 ---
    public class OriginalLanguageImageProvider : IRemoteImageProvider // Remove , IHasOrder
    {
        // Name 属性仍然需要，用于 UI 显示和日志
        public string Name => "OriginalPoster Provider";

        // --- 移除 Order 属性 ---
        // public int Order => 999; // Remove this line
        // ---


        // ... (保持 Supports, GetImages, GetSupportedImages, GetImageResponse 方法不变)
        // ... (保持构造函数注入 ILogManager 和 IHttpClient 不变)
        // ... (保持所有日志记录不变)

        private readonly ILogger _logger; // Use Emby's ILogger
        private readonly IHttpClient _httpClient; // Use Emby's IHttpClient

        public OriginalLanguageImageProvider(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _logger.Info("OriginalLanguageImageProvider constructor called (No IHasOrder).");
        }


        public bool Supports(BaseItem item)
        {
            _logger.Debug("Supports called for item: {ItemName}, Type: {ItemType}", item.Name, item.GetType().Name);
            var isSupported = item is Movie movie && movie.HasProviderId(MetadataProviders.Tmdb);
            _logger.Debug("Supports result for '{ItemName}': {IsSupported}", item.Name, isSupported);
            return isSupported;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            if (!(item is Movie movie))
            {
                _logger.Debug("Item '{ItemName}' is not a Movie, skipping.", item.Name);
                return new List<RemoteImageInfo>();
            }

            _logger.Info("GetImages called for movie: {MovieName} (No IHasOrder).", movie.Name);

            if (!Plugin.Instance.Configuration.EnablePlugin)
            {
                _logger.Debug("Plugin is disabled, skipping.");
                return new List<RemoteImageInfo>();
            }

            var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger.Debug("Movie '{MovieName}' does not have a TMDB ID. Skipping.", movie.Name);
                return new List<RemoteImageInfo>();
            }

            var apiKey = Plugin.Instance.Configuration.TmdbApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                 _logger.Warn("TMDB API Key is not configured. Skipping for movie: {MovieName}", movie.Name);
                 return new List<RemoteImageInfo>();
            }

            _logger.Debug("Processing movie '{MovieName}' with TMDB ID {TmdbId}.", movie.Name, tmdbId);

            try
            {
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not determine original language for TMDB ID {TmdbId}. Skipping.", tmdbId);
                    return new List<RemoteImageInfo>();
                }

                _logger.Info("Original language for TMDB ID {TmdbId} ({MovieName}) is {OriginalLanguage}.", tmdbId, movie.Name, originalLanguage);

                var sortedPosters = await GetSortedPostersAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                _logger.Info("Returning {Count} sorted posters for TMDB ID {TmdbId} ({MovieName}).", sortedPosters.Count, tmdbId, movie.Name);
                return sortedPosters;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"An error occurred while processing movie '{movie.Name}' (TMDB ID {TmdbId}).", ex);
                return new List<RemoteImageInfo>();
            }
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            _logger.Debug("GetSupportedImages called for item: {ItemName}", item.Name);
            return new[] { ImageType.Primary };
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.Debug("GetImageResponse called for URL: {Url}", url);
            return Task.FromResult(new HttpResponseInfo());
        }

        // --- Helper Methods (Keep them as they are, or simplify if needed for this test) ---
        // For this test, you can keep the full logic or temporarily simplify GetImages to return an empty list
        // to see if the provider appears in the UI *before* adding back the complex logic.
        // Let's keep the logic for now, as the test is about whether the provider is recognized *at all*.
        private async Task<string?> GetOriginalLanguageAsync(string tmdbId, string apiKey, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}&language=en-US";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true
            };

            _logger.Debug("Fetching original language from TMDB for ID {TmdbId} using URL: {Url}", tmdbId, url);

            using var response = await _httpClient.GetResponse(options);
            using var stream = response.Content;
            using var reader = new StreamReader(stream);

            var jsonString = await reader.ReadToEndAsync();
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.TryGetProperty("original_language", out var langElement))
            {
                var lang = langElement.GetString();
                _logger.Debug("Fetched original language '{Language}' for TMDB ID {TmdbId}.", lang, tmdbId);
                return lang;
            }

            _logger.Warn("Could not find 'original_language' property in TMDB response for ID {TmdbId}.", tmdbId);
            return null;
        }

        private async Task<List<RemoteImageInfo>> GetSortedPostersAsync(string tmdbId, string apiKey, string originalLanguage, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/images?api_key={apiKey}";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true
            };

            _logger.Debug("Fetching posters from TMDB for ID {TmdbId} using URL: {Url}", tmdbId, url);

            using var response = await _httpClient.GetResponse(options);
            using var stream = response.Content;
            using var reader = new StreamReader(stream);

            var jsonString = await reader.ReadToEndAsync();
            using var doc = JsonDocument.Parse(jsonString);

            var postersArray = doc.RootElement.GetProperty("posters").EnumerateArray();

            var matchingPosters = new List<RemoteImageInfo>();
            var otherPosters = new List<RemoteImageInfo>();

            foreach (var poster in postersArray)
            {
                var isoCode = poster.GetProperty("iso_639_1").GetString(); // Can be null
                var filePath = poster.GetProperty("file_path").GetString();
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Debug("Skipping poster with empty file_path for TMDB ID {TmdbId}.", tmdbId);
                    continue;
                }

                var fullImageUrl = $"https://image.tmdb.org/t/p/original{filePath}";
                var imageInfo = new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = fullImageUrl,
                    Type = ImageType.Primary,
                    Language = isoCode
                };

                if (isoCode == originalLanguage)
                {
                    matchingPosters.Add(imageInfo);
                    _logger.Debug("Added matching poster (Lang: {Lang}) for TMDB ID {TmdbId}: {Url}", isoCode, tmdbId, fullImageUrl);
                }
                else
                {
                    otherPosters.Add(imageInfo);
                    _logger.Debug("Added other poster (Lang: {Lang}) for TMDB ID {TmdbId}: {Url}", isoCode, tmdbId, fullImageUrl);
                }
            }

            var sortedList = new List<RemoteImageInfo>(matchingPosters);
            sortedList.AddRange(otherPosters);

            _logger.Info("Sorted {TotalCount} posters for TMDB ID {TmdbId}: {MatchingCount} matching original language '{OriginalLanguage}', {OtherCount} others.", sortedList.Count, tmdbId, matchingPosters.Count, originalLanguage, otherPosters.Count);

            return sortedList;
        }
    }
}