#nullable enable // 启用可空引用类型检查

using MediaBrowser.Controller.Entities; // BaseItem
using MediaBrowser.Controller.Entities.Movies; // For Movie type
using MediaBrowser.Controller.Providers; // For IMetadataProvider, ItemLookupInfo, etc.
using MediaBrowser.Model.Entities; // For ImageType, ProviderIdDictionary, etc.
using MediaBrowser.Model.Providers; // For MetadataResult, RemoteImageInfo
using MediaBrowser.Model.Logging; // For ILogger
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net; // For IHttpClient, HttpRequestOptions, HttpResponseInfo
using System.Text.Json; // For JSON parsing
using System.IO; // For Stream
using System;
using MediaBrowser.Controller.Entities; // For BaseItem
using MediaBrowser.Model.IO; // For FileSystemMetadata

namespace OriginalPoster.Providers
{
    public class OriginalLanguageMetadataProvider : IMetadataProvider<Movie>
    {
        public string Name => "OriginalPoster Metadata Provider";

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public OriginalLanguageMetadataProvider(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _logger.Info("OriginalLanguageMetadataProvider constructor called.");
        }

        public bool Supports(BaseItem item)
        {
            // Only support Movie items for this provider
            return item is Movie;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(ItemLookupInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.Debug("GetMetadata called for info.Name: {InfoName}, ProviderIds: {@ProviderIds}", info.Name, info.ProviderIds);

            // 1. Check if the plugin is enabled via configuration
            if (!Plugin.Instance.Configuration.EnablePlugin)
            {
                _logger.Debug("Plugin is disabled, skipping metadata fetch.");
                return new MetadataResult<Movie>();
            }

            // 2. Get TMDB ID from the lookup info
            if (!info.ProviderIds.TryGetValue(MetadataProviders.Tmdb.ToString(), out string tmdbId) || string.IsNullOrEmpty(tmdbId))
            {
                _logger.Debug("Item lookup info does not have a TMDB ID. Skipping metadata fetch for {InfoName}.", info.Name);
                return new MetadataResult<Movie>();
            }

            // 3. Get user-configured TMDB API Key
            var apiKey = Plugin.Instance.Configuration.TmdbApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                 _logger.Warn("TMDB API Key is not configured. Skipping metadata fetch for {InfoName}.", info.Name);
                 return new MetadataResult<Movie>(); // API Key is required
            }

            _logger.Debug("Processing metadata for movie '{InfoName}' with TMDB ID {TmdbId}.", info.Name, tmdbId);

            var metadataResult = new MetadataResult<Movie>();
            metadataResult.Item = new Movie(); // Initialize the item
            metadataResult.Item.SetProviderId(MetadataProviders.Tmdb, tmdbId); // Set the TMDB ID on the new item

            try
            {
                // 4. Get the original language
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not determine original language for TMDB ID {TmdbId}. Skipping image processing.", tmdbId);
                    // Still return an empty result, but the metadata part is initialized
                    return metadataResult;
                }

                _logger.Debug("Original language for TMDB ID {TmdbId} is {OriginalLanguage}.", tmdbId, originalLanguage);

                // 5. Get and sort posters based on original language
                var sortedRemoteImageInfos = await GetSortedPostersAsRemoteImageInfosAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                // 6. Convert RemoteImageInfo to LocalImageInfo and assign to metadata result
                // This is the key step that injects the prioritized images into the metadata process
                var localImageInfos = new List<LocalImageInfo>();
                foreach (var remoteInfo in sortedRemoteImageInfos)
                {
                    // Create a FileSystemMetadata object to represent the remote image file
                    var fileSystemMetadata = new FileSystemMetadata
                    {
                        FullName = remoteInfo.Url, // Use the URL as the FullName for remote images
                        IsDirectory = false, // It's a file, not a directory
                        Exists = true // Assume it exists for now; Emby will handle download later
                    };

                    // Create a LocalImageInfo and set its FileInfo to the FileSystemMetadata
                    var localInfo = new LocalImageInfo
                    {
                        FileInfo = fileSystemMetadata, // This is the correct assignment
                        Type = remoteInfo.Type // Set the image type directly on LocalImageInfo
                    };
                    localImageInfos.Add(localInfo);
                }

                metadataResult.Images = localImageInfos; // Assign the converted list

                _logger.Info("Assigned {Count} sorted posters (converted to LocalImageInfo) to metadata result for TMDB ID {TmdbId}.", localImageInfos.Count, tmdbId);
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"An error occurred while processing metadata for '{info.Name}' (TMDB ID {tmdbId}).", ex);
                // Return an empty result on error, but log the issue
                return new MetadataResult<Movie>();
            }

            return metadataResult;
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
            return null; // Or throw an exception if the property is expected to always exist
        }

        /// <summary>
        /// Fetches all posters from TMDB API, sorts them based on the original language,
        /// and converts them to RemoteImageInfo objects (which can then be converted to LocalImageInfo).
        /// </summary>
        private async Task<List<RemoteImageInfo>> GetSortedPostersAsRemoteImageInfosAsync(string tmdbId, string apiKey, string originalLanguage, CancellationToken cancellationToken)
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
                if (string.IsNullOrEmpty(filePath)) continue; // Skip posters without a file path

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