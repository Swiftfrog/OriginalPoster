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
        public int Order => 999; // Try a very high order

        private readonly ILogger _logger; // Use Emby's ILogger
        // Remove IHttpClient field

        // --- Remove IHttpClient from constructor ---
        public OriginalLanguageImageProvider(ILogManager logManager)
        {
            // 从 ILogManager 获取 logger
            _logger = logManager.GetLogger(GetType().Name);
            // 记录构造函数被调用的日志
            _logger.Info("OriginalLanguageImageProvider constructor called (No HttpClient).");
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
        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            // Cast to Movie since our Supports method ensures this
            if (!(item is Movie movie))
            {
                _logger.Debug("Item '{ItemName}' is not a Movie, skipping.", item.Name);
                return Task.FromResult<IEnumerable<RemoteImageInfo>>(new List<RemoteImageInfo>());
            }

            _logger.Info("GetImages called for movie: {MovieName} (No HttpClient).", movie.Name);

            // For this test, just return an empty list or a dummy list
            var dummyList = new List<RemoteImageInfo>();
            _logger.Info("Returning {Count} dummy images for movie: {MovieName} (No HttpClient).", dummyList.Count, movie.Name);
            return Task.FromResult<IEnumerable<RemoteImageInfo>>(dummyList);
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
            _logger.Debug("GetImageResponse called for URL: {Url} (No HttpClient).", url);
            // Returning a completed task with a default HttpResponseInfo.
            // Emby should handle the download of the URL provided in GetImages.
            return Task.FromResult(new HttpResponseInfo());
        }

        // --- Remove Helper Methods (GetOriginalLanguageAsync, GetSortedPostersAsync) ---
    }
}