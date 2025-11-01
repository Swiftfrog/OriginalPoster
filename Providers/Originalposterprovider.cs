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
using MediaBrowser.Model.Serialization; // 新增 using
using OriginalPoster.Models;           // 新增 using
using OriginalPoster.Services;         // 新增 using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster.Providers
{
    /// <summary>
    /// 原语言海报提供者 - 第二阶段：支持从 TMDB 获取固定语言（英文）海报
    /// </summary>
    public class OriginalPosterProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer; // 新增字段

        public string Name => "TMDB Original Language";
        public int Order => 0;

        // 修改构造函数：注入 IJsonSerializer
        public OriginalPosterProvider(IHttpClient httpClient, ILogger logger, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _logger?.Info("[OriginalPoster] Provider initialized with JsonSerializer");
        }

        public bool Supports(BaseItem item)
        {
            var supported = item is Movie;
            _logger.Debug("[OriginalPoster] Supports check for {0}: {1}", item.Name, supported);
            return supported;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(
            BaseItem item,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            _logger?.Debug("[OriginalPoster] GetImages called for: {0}", item.Name);
            
            // 默认返回空列表
            var images = Array.Empty<RemoteImageInfo>();

            // === 第一阶段：测试模式 ===
            if (config?.TestMode == true)
            {
                _logger?.Debug("[OriginalPoster] Test mode enabled, returning test poster");
                var testImage = new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = config.TestPosterUrl.Trim(), // 防止多余空格
                    ThumbnailUrl = config.TestPosterUrl.Trim(),
                    Language = "zh",
                    DisplayLanguage = "Chinese",
                    Width = 1000,
                    Height = 1500,
                    CommunityRating = 8.5,
                    VoteCount = 100,
                    RatingType = RatingType.Score
                };
                _logger?.Debug("[OriginalPoster] Returning 1 test image");
                return new[] { testImage };
            }

            // === 第二阶段：真实 TMDB 调用 ===
            var tmdbId = GetTmdbId(item);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger?.Debug("[OriginalPoster] No TMDB ID found for item, skipping");
                return images; // ✅ 显式返回
            }

            try
            {
                // 固定语言：第二阶段目标为 "en"
                const string targetLanguage = "en";
                _logger?.Debug("[OriginalPoster] Fetching images for TMDB ID: {0}, language: {1}", tmdbId, targetLanguage);

                var tmdbClient = new TmdbClient(_httpClient, _jsonSerializer, config.TmdbApiKey);
                var result = await tmdbClient.GetImagesAsync(tmdbId, item is Movie, targetLanguage, cancellationToken);

                images = ConvertToRemoteImageInfo(result, targetLanguage); // ✅ 关键修复：赋值给 images
                _logger?.Debug("[OriginalPoster] Fetched {0} images from TMDB", remoteImages.Count());

                // return remoteImages;
            }
            catch (Exception ex)
            {
                //_logger?.Error(ex, "[OriginalPoster] Failed to fetch images from TMDB for {0}", item.Name);
                _logger?.Error("[OriginalPoster] Failed to fetch images from TMDB for {0}", item.Name);
            }
            
            return images; // ✅ 统一返回点
            
        }

        private string GetTmdbId(BaseItem item)
        {
            if (item.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var id) == true)
            {
                return id;
            }
            return null;
        }

        private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(TmdbImageResult tmdbResult, string fallbackLanguage)
        {
            if (tmdbResult?.posters == null || tmdbResult.posters.Length == 0)
                return Array.Empty<RemoteImageInfo>();

            var list = new List<RemoteImageInfo>();
            foreach (var poster in tmdbResult.posters)
            {
                if (string.IsNullOrEmpty(poster.file_path))
                    continue;

                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = $"https://image.tmdb.org/t/p/original{poster.file_path}",
                    ThumbnailUrl = $"https://image.tmdb.org/t/p/w500{poster.file_path}",
                    Language = poster.iso_639_1 ?? fallbackLanguage,
                    DisplayLanguage = GetDisplayLanguage(poster.iso_639_1 ?? fallbackLanguage),
                    Width = poster.width,
                    Height = poster.height,
                    CommunityRating = poster.vote_average,
                    VoteCount = poster.vote_count,
                    RatingType = RatingType.Score
                });
            }

            if (list.Count > 0)
            {
                _logger?.Info("[OriginalPoster] First image URL: {0}", list[0].Url);
            }

            // 按评分降序排列
            return list.OrderByDescending(img => img.CommunityRating ?? 0);
        }

        private string GetDisplayLanguage(string langCode)
        {
            // 简单映射，后续可扩展
            return langCode switch
            {
                "en" => "English",
                "zh" => "Chinese",
                "ja" => "Japanese",
                "ko" => "Korean",
                _ => langCode.ToUpperInvariant()
            };
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public bool Supports(BaseItem item, ImageType imageType)
        {
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