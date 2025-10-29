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
        public string Name => "OriginalPoster Test Provider"; // 修改名称，方便识别
        public int Order => 100; // Ensure it runs after TMDB provider

        private readonly ILogger _logger; // Use Emby's ILogger

        // --- 简化构造函数：只注入 ILogManager ---
        public OriginalLanguageImageProvider(ILogManager logManager)
        {
            // 从 ILogManager 获取 logger
            _logger = logManager.GetLogger(GetType().Name);
        }

        // Supports method now takes BaseItem
        public bool Supports(BaseItem item)
        {
            // Only process Movie items with a TMDB ID
            // This is the key check for our plugin's scope.
            var isMovie = item is Movie;
            var hasTmdbId = item.HasProviderId(MetadataProviders.Tmdb);
            var result = isMovie && hasTmdbId;

            if (result)
            {
                _logger.Debug("Supports: Item '{ItemName}' is a Movie with TMDB ID. Provider will be considered.", item.Name);
            }
            else
            {
                _logger.Debug("Supports: Item '{ItemName}' (Type: {ItemType}) does not meet criteria. IsMovie: {IsMovie}, HasTmdbId: {HasTmdbId}", item.Name, item.GetType().Name, isMovie, hasTmdbId);
            }

            return result;
        }

        // GetImages method now takes BaseItem and LibraryOptions
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            // Cast to Movie since our Supports method ensures this
            if (!(item is Movie movie))
            {
                _logger.Debug("GetImages: Item '{ItemName}' is not a Movie, skipping.", item.Name);
                return new List<RemoteImageInfo>();
            }

            _logger.Info("GetImages CALLED for movie: {MovieName} (TMDB ID: {TmdbId}). This confirms the provider is being invoked by Emby.", movie.Name, movie.GetProviderId(MetadataProviders.Tmdb));

            // --- "Hello World" Logic: Return a single, identifiable test image ---
            // Use a simple, static image URL that you can easily recognize.
            // Example: A placeholder image service
            var testImageUrl = "https://via.placeholder.com/1000x1500/FF0000/FFFFFF?text=OriginalPoster+Test+Image";

            var testImageInfo = new RemoteImageInfo
            {
                ProviderName = Name, // Use the provider's Name property
                Url = testImageUrl,
                Type = ImageType.Primary, // Or Primary, depending on desired Emby image type
                Language = "test" // Set a test language code
            };

            // Log the image being returned
            _logger.Info("GetImages: Returning test image URL: {ImageUrl} for movie: {MovieName}", testImageUrl, movie.Name);

            // Return a list containing only our test image
            // Emby will take the first image in the list if this provider is selected and has high enough priority.
            return new List<RemoteImageInfo> { testImageInfo };

            // --- Original complex logic is commented out for this test ---
            // if (!Plugin.Instance.Configuration.EnablePlugin) { ... }
            // var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb); ...
            // ... (rest of complex logic)
        }

        // --- Required by IRemoteImageProvider ---

        /// <summary>
        /// Defines which types of images this provider supports for this item.
        /// </summary>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            // We provide Primary images (which Emby often uses for main posters)
            _logger.Debug("GetSupportedImages called for item: {ItemName}", item.Name);
            return new[] { ImageType.Primary };
        }

        /// <summary>
        /// Fetches the image response stream. Not used by us since we provide URLs via GetImages.
        /// </summary>
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            // Returning a completed task with a default HttpResponseInfo.
            // Emby should handle the download of the URL provided in GetImages.
            _logger.Debug("GetImageResponse called for URL: {ImageUrl}. Returning default response.", url);
            return Task.FromResult(new HttpResponseInfo());
        }
    }
}
