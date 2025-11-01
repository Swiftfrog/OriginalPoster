// Originalposterprovider.cs
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        
        public OriginalPosterProvider(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _logger?.Info("[OriginalPoster] Provider initialized");
        }
        
        /// <summary>
        /// 判断是否支持该媒体项
        /// </summary>
        public bool Supports(BaseItem item)
        {
            // 第一阶段只支持电影
            var supported = item is Movie;
            
            // _logger?.Debug("[OriginalPoster]: Supports check for {item.Name}: {supported}");
            _logger.Debug("[OriginalPoster] Supports check for {0}: {1}", item.Name, supported);
            
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
            
            // _logger?.Debug("[OriginalPoster]: GetImages called for: {item.Name}");
            _logger?.Debug("[OriginalPoster] GetImages called for: {0}", item.Name);
        
            var images = new List<RemoteImageInfo>();
            
            if (config?.TestMode == true)
            {
                _logger?.Debug("[OriginalPoster] Test mode enabled, returning test poster");
                
                images.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = config.TestPosterUrl,
                    ThumbnailUrl = config.TestPosterUrl,
                    Language = "zh",
                    DisplayLanguage = "Chinese",
                    Width = 1000,
                    Height = 1500,
                    CommunityRating = 8.5,
                    VoteCount = 100,
                    RatingType = RatingType.Score
                });
                
                _logger?.Debug("[OriginalPoster] Returning {0} test image(s)", images.Count);
            }
            else
            {
                _logger?.Info("[OriginalPoster] Test mode disabled, returning empty list");
            }
            
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
        
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }

    }
}

