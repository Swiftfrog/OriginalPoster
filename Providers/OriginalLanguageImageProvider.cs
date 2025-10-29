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
    // --- 关键修改：实现 IRemoteImageProvider<Movie> (泛型) ---
    public class OriginalLanguageImageProvider : IRemoteImageProvider<Movie>, IHasOrder
    {
        public string Name => "OriginalPoster Provider";
        public int Order => 100; // Ensure it runs after TMDB provider

        private readonly ILogger _logger; // Use Emby's ILogger

        // --- 构造函数：只使用 ILogManager ---
        public OriginalLanguageImageProvider(ILogManager logManager)
        {
            // 从 ILogManager 获取 logger
            _logger = logManager.GetLogger(GetType().Name);
            // 记录构造函数被调用的日志
            _logger.Info("OriginalLanguageImageProvider<Movie> constructor called.");
        }
        // ---


        // Supports method now takes Movie (because of IRemoteImageProvider<Movie>)
        public bool Supports(Movie item)
        {
            _logger.Debug("Supports called for movie: {MovieName}", item.Name);
            // Only process Movie items with a TMDB ID
            var isSupported = item.HasProviderId(MetadataProviders.Tmdb);
            _logger.Debug("Supports result: {IsSupported}", isSupported);
            return isSupported;
        }

        // GetImages method now takes Movie and LibraryOptions (because of IRemoteImageProvider<Movie>)
        public Task<IEnumerable<RemoteImageInfo>> GetImages(Movie item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            _logger.Info("GetImages called for movie: {MovieName}", item.Name);

            // Since we are simplifying, just return an empty list
            // This should still appear in the logs if the provider is called
            var emptyList = new List<RemoteImageInfo>();
            _logger.Info("GetImages returning {Count} images for movie: {MovieName}", emptyList.Count, item.Name);
            return Task.FromResult<IEnumerable<RemoteImageInfo>>(emptyList);
        }

        // --- Required by IRemoteImageProvider<Movie> (inherits from IRemoteImageProvider) ---

        /// <summary>
        /// Defines which types of images this provider supports for this item.
        /// </summary>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            _logger.Debug("GetSupportedImages called for item: {ItemName}", item.Name);
            // We provide Primary images (which Emby often uses for main posters)
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
    }
}