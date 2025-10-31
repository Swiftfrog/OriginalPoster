using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MediaBrowser.Model.Logging;

namespace OriginalPoster.Providers
{
    /// <summary>
    /// 原语言海报提供者 - 第一阶段基础实现
    /// </summary>
    public class OriginalPosterProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        
        /// <summary>
        /// 提供者名称（显示在媒体库设置中）
        /// </summary>
        public string Name => "TMDB Original Language";
        
        /// <summary>
        /// 优先级顺序（0最高）
        /// </summary>
        public int Order => 0;
        
        public OriginalPosterProvider(IHttpClient httpClient, ILogManager logManager)
        {
            _httpClient = httpClient;
            // 👇 直接使用官方 NullLogger
            // _logger = logManager?.GetLogger(GetType().Name) ?? NullLogger.Instance;
            _logger = logManager?.GetLogger(GetType().Name) ?? new NullLogger();
            LogDebug("Provider initialized");
        }
        
        /// <summary>
        /// 判断是否支持该媒体项
        /// </summary>
        public bool Supports(BaseItem item)
        {
            // 第一阶段只支持电影
            var supported = item is Movie;
            
            if (supported)
            {
                LogDebug($"Supports check for {item.Name}: {supported}");
            }
            
            return supported;
        }
        
        /// <summary>
        /// 获取图像列表 - 第一阶段返回测试数据
        /// </summary>
        public Task<IEnumerable<RemoteImageInfo>> GetImages(
            BaseItem item, 
            LibraryOptions libraryOptions, 
            CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            
            LogInfo($"GetImages called for: {item.Name} (Path: {item.Path})");
            
            var images = new List<RemoteImageInfo>();
            
            // 检查配置
            if (config == null)
            {
                LogDebug("Configuration is null");
                return Task.FromResult<IEnumerable<RemoteImageInfo>>(images);
            }
            
            if (!config.Enabled)
            {
                LogDebug("Plugin is disabled");
                return Task.FromResult<IEnumerable<RemoteImageInfo>>(images);
            }
            
            if (config.TestMode)
            {
                LogInfo("Test mode enabled, returning test poster");
                
                var testUrl = config.TestPosterUrl;
                if (string.IsNullOrWhiteSpace(testUrl))
                {
                    // 使用默认的测试海报
                    testUrl = "https://image.tmdb.org/t/p/original/zGINvGjdlO6TJRu9wESQvWlOKVT.jpg";
                }
                
                images.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,  // Primary = 海报
                    Url = testUrl,
                    ThumbnailUrl = testUrl
                });
                
                LogInfo($"Returning test poster: {testUrl}");
            }
            else
            {
                LogDebug("Test mode disabled, returning empty list");
            }
            
            LogDebug($"Total images to return: {images.Count}");
            return Task.FromResult<IEnumerable<RemoteImageInfo>>(images);
        }
        
        /// <summary>
        /// 获取支持的图像类型
        /// </summary>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            // 只支持海报
            return new[] { ImageType.Primary };
        }
        
        /// <summary>
        /// 是否支持该图像类型
        /// </summary>
        public bool Supports(BaseItem item, ImageType imageType)
        {
            // 只支持Primary（海报）类型
            return imageType == ImageType.Primary && Supports(item);
        }
        
        /// <summary>
        /// 获取图像响应
        /// </summary>
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            LogDebug($"GetImageResponse called for URL: {url}");
            
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = false
            });
        }
        
        /// <summary>
        /// 信息日志
        /// </summary>
        private void LogInfo(string message)
        {
            _logger?.Info($"[OriginalPoster] {message}");
            Console.WriteLine($"[OriginalPoster] INFO: {DateTime.Now:HH:mm:ss} {message}");
        }
        
        /// <summary>
        /// 调试日志
        /// </summary>
        private void LogDebug(string message)
        {
            if (Plugin.Instance?.Configuration?.DebugLogging == true)
            {
                _logger?.Debug($"[OriginalPoster] {message}");
                Console.WriteLine($"[OriginalPoster] DEBUG: {DateTime.Now:HH:mm:ss} {message}");
            }
        }
    }
    
}
