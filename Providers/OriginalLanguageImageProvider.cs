using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities; // For ImageType
using MediaBrowser.Model.Logging; // For ILogger (Emby's standard)
using MediaBrowser.Model.Providers; // For RemoteImageInfo, IHasOrder
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net; // For IHttpClient, HttpRequestOptions, HttpResponseInfo
using System.Text.Json; // For JSON parsing
using System.IO; // For Stream

namespace OriginalPoster.Providers
{
    public class OriginalLanguageImageProvider : IRemoteImageProvider<Movie>, IHasOrder
    {
        public string Name => "OriginalPoster Provider";
        public int Order => 100; // Ensure it runs after TMDB provider

        private readonly ILogger _logger; // Use Emby's ILogger
        private readonly IHttpClient _httpClient;

        public OriginalLanguageImageProvider(
            ILogger logger, // Inject Emby's ILogger
            IHttpClient httpClient
        )
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public bool Supports(Movie item)
        {
            return item.HasProviderId(MetadataProviders.Tmdb);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(Movie item, CancellationToken cancellationToken)
        {
            _logger.Debug("GetImages called for movie: {MovieName}", item.Name);

            if (!Plugin.Instance.Configuration.EnablePlugin)
            {
                _logger.Debug("Plugin is disabled, skipping.");
                return new List<RemoteImageInfo>();
            }

            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger.Debug("Movie '{MovieName}' does not have a TMDB ID. Skipping.", item.Name);
                return new List<RemoteImageInfo>();
            }

            var apiKey = Plugin.Instance.Configuration.TmdbApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                 _logger.Warn("TMDB API Key is not configured. Skipping for movie: {MovieName}", item.Name);
                 return new List<RemoteImageInfo>(); // API Key is required
            }

            _logger.Debug("Processing movie '{MovieName}' with TMDB ID {TmdbId}.", item.Name, tmdbId);

            try
            {
                // 1. Get the original language
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not determine original language for TMDB ID {TmdbId}. Skipping.", tmdbId);
                    return new List<RemoteImageInfo>();
                }

                _logger.Debug("Original language for TMDB ID {TmdbId} is {OriginalLanguage}.", tmdbId, originalLanguage);

                // 2. Get and sort posters based on original language
                var sortedPosters = await GetSortedPostersAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                _logger.Debug("Returning {Count} sorted posters for TMDB ID {TmdbId}.", sortedPosters.Count, tmdbId);
                return sortedPosters;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"An error occurred while processing movie '{item.Name}' (TMDB ID {tmdbId}).", ex);
                // Or use a more structured logging if available in MediaBrowser.Model.Logging
                // _logger.Error(ex, "An error occurred while processing movie '{MovieName}' (TMDB ID {TmdbId}).", item.Name, tmdbId);
                return new List<RemoteImageInfo>(); // Return empty list on error
            }
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

            using var response = await _httpClient.GetResponse(options);
            using var stream = response.Content;
            using var reader = new StreamReader(stream);

            var jsonString = await reader.ReadToEndAsync();
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.TryGetProperty("original_language", out var langElement))
            {
                return langElement.GetString();
            }

            return null; // Or throw an exception if the property is expected to always exist
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
                }
                else
                {
                    otherPosters.Add(imageInfo);
                }
            }

            // Combine lists: matching first, then others
            var sortedList = new List<RemoteImageInfo>(matchingPosters);
            sortedList.AddRange(otherPosters);

            return sortedList;
        }
    }
}
