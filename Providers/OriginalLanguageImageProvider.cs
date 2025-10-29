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
    public class OriginalLanguageImageProvider : IRemoteImageProvider, IHasOrder
    {
        public string Name => "OriginalPoster Provider";
        public int Order => 5; // Ensure it runs after TMDB provider

        private readonly ILogger _logger; // Use Emby's ILogger
        private readonly IHttpClient _httpClient; // Use Emby's IHttpClient

        // --- 恢复 IHttpClient 注入 ---
        public OriginalLanguageImageProvider(ILogManager logManager, IHttpClient httpClient)
        {
            // 从 ILogManager 获取 logger
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            // 记录构造函数被调用的日志
            _logger.Info("OriginalLanguageImageProvider constructor called.");
        }
        // ---


        // Supports method now takes BaseItem
        public bool Supports(BaseItem item)
        {
            _logger.Debug("Supports called for item: {ItemName}, Type: {ItemType}", item.Name, item.GetType().Name);
            // Only process Movie items with a TMDB ID
            var isSupported = item is Movie movie && movie.HasProviderId(MetadataProviders.Tmdb);
            _logger.Debug("Supports result for '{ItemName}': {IsSupported}", item.Name, isSupported);
            return isSupported;
        }

        // GetImages method now takes BaseItem and LibraryOptions
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            // Cast to Movie since our Supports method ensures this
            if (!(item is Movie movie))
            {
                _logger.Debug("Item '{ItemName}' is not a Movie, skipping.", item.Name);
                return new List<RemoteImageInfo>();
            }

            _logger.Info("GetImages called for movie: {MovieName}", movie.Name);

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
                 return new List<RemoteImageInfo>(); // API Key is required
            }

            _logger.Debug("Processing movie '{MovieName}' with TMDB ID {TmdbId}.", movie.Name, tmdbId);

            try
            {
                // 1. Get the original language
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not determine original language for TMDB ID {TmdbId}. Skipping.", tmdbId);
                    return new List<RemoteImageInfo>();
                }

                _logger.Info("Original language for TMDB ID {TmdbId} ({MovieName}) is {OriginalLanguage}.", tmdbId, movie.Name, originalLanguage);

                // 2. Get and sort posters based on original language
                var sortedPosters = await GetSortedPostersAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                _logger.Info("Returning {Count} sorted posters for TMDB ID {TmdbId} ({MovieName}).", sortedPosters.Count, tmdbId, movie.Name);
                return sortedPosters;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"An error occurred while processing movie '{movie.Name}' (TMDB ID {tmdbId}).", ex);
                return new List<RemoteImageInfo>(); // Return empty list on error
            }
        }

        // --- Required by IRemoteImageProvider ---

        /// <summary>
        /// Defines which types of images this provider supports for this item.
        /// </summary>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            _logger.Debug("GetSupportedImages called for item: {ItemName}", item.Name);
            return new[] { ImageType.Primary };
        }

        /// <summary>
        /// Fetches the image response stream. Not used by us since we provide URLs via GetImages.
        /// </summary>
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.Debug("GetImageResponse called for URL: {Url}", url);
            // Returning a completed task with a default HttpResponseInfo.
            // Emby should handle the download of the URL provided in GetImages.
            return Task.FromResult(new HttpResponseInfo());
        }


        // --- Helper Methods ---

        /// <summary>
        /// Fetches the original language of a movie from TMDB API.
        /// </summary>
        private async Task<string?> GetOriginalLanguageAsync(string tmdbId, string apiKey, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}&language=en-US";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true // Buffer the response content
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

        /// <summary>
        /// Fetches all posters from TMDB API and sorts them based on the original language.
        /// </summary>
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
                    continue; // Skip posters without a file path
                }

                var fullImageUrl = $"https://image.tmdb.org/t/p/original{filePath}";
                var imageInfo = new RemoteImageInfo
                {
                    ProviderName = Name, // Use the provider's Name property
                    Url = fullImageUrl,
                    Type = ImageType.Primary, // Or Primary, depending on desired Emby image type
                    Language = isoCode // Set the language code for potential future use by Emby
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

            // Combine lists: matching first, then others
            var sortedList = new List<RemoteImageInfo>(matchingPosters);
            sortedList.AddRange(otherPosters);

            _logger.Info("Sorted {TotalCount} posters for TMDB ID {TmdbId}: {MatchingCount} matching original language '{OriginalLanguage}', {OtherCount} others.", sortedList.Count, tmdbId, matchingPosters.Count, originalLanguage, otherPosters.Count);

            return sortedList;
        }
    }
}