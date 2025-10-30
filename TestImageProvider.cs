using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace OriginalPoster
{
    /// <summary>
    /// 简化测试版本 - 直接放在主命名空间下
    /// </summary>
    public class TestImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public string Name => "TestOriginalPoster";
        public int Order => -100;

        public TestImageProvider(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger("TestOriginalPoster");
            _httpClient = httpClient;
            _logger.Info("███████████████████████████████████████████████████████████");
            _logger.Info("███ TestImageProvider CONSTRUCTOR CALLED! ███");
            _logger.Info("███████████████████████████████████████████████████████████");
        }

        public bool Supports(BaseItem item)
        {
            var result = item is Movie && item.HasProviderId(MetadataProviders.Tmdb);
            if (result)
            {
                _logger.Info("TestImageProvider.Supports = TRUE for: {0}", item.Name);
            }
            return result;
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            _logger.Info("███████████████████████████████████████████████████████████");
            _logger.Info("███ TestImageProvider.GetImages CALLED! ███");
            _logger.Info("███ Item: {0}", item.Name);
            _logger.Info("███████████████████████████████████████████████████████████");
            
            // 返回空列表，仅用于测试是否被调用
            return Task.FromResult<IEnumerable<RemoteImageInfo>>(new List<RemoteImageInfo>());
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseInfo());
        }
    }
}
