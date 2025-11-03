// Providers/Originalposterprovider.cs
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using OriginalPoster.Models;
using OriginalPoster.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster.Providers
{
    public class OriginalPosterProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public string Name => "OriginalPosterTMDB";
        public int Order => 0;

        public OriginalPosterProvider(IHttpClient httpClient, ILogger logger, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _logger?.Info("[OriginalPoster] Provider initialized with JsonSerializer");
        }

        public bool Supports(BaseItem item)
        {
//            var supported = item is Movie;
//            var supported = item is Movie || item is Series; // ✅ 支持电影和剧集
            var supported = item is Movie || item is Series || item is Season;
            _logger.Debug("[OriginalPoster] Supports check for {0}: {1}", item.Name, supported);
            return supported;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(
            BaseItem item,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            
            // 全局开关：插件是否启用
            if (config?.Enabled != true)
            {
                _logger?.Debug("[OriginalPoster] Please Enable Plugin");
                return Enumerable.Empty<RemoteImageInfo>();
            }
            
            _logger?.Debug("[OriginalPoster] GetImages called for: {0}", item.Name);

            var images = Enumerable.Empty<RemoteImageInfo>();

            // === 第一阶段：测试模式 ===
            if (config?.TestMode == true)
            {
                _logger?.Debug("[OriginalPoster] Test mode enabled, returning test poster");
                
                string testlangCode = !string.IsNullOrWhiteSpace(config.MetadataLanguage) 
                    ? config.MetadataLanguage.Trim() // 顺便 Trim 一下
                    : "en";
                
                var testImage = new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = config.TestPosterUrl.Trim(),
                    ThumbnailUrl = config.TestPosterUrl.Trim(),
                    Language = testlangCode,
                    DisplayLanguage = GetDisplayLanguage(testlangCode),
                    Width = 2000,
                    Height = 3000,
                    CommunityRating = 10,
                    VoteCount = 100,
                    RatingType = RatingType.Score
                };
                _logger?.Debug("[OriginalPoster] Returning 1 test image");
                return new[] { testImage };
            }
            // === 第一阶段：测试模式 ===

            // === 自动语言识别 ===
            var tmdbId = GetTmdbId(item);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger?.Debug("[OriginalPoster] No TMDB ID found for item, skipping");
                return images;
            }

            try
            {
                var tmdbClient = new TmdbClient(_httpClient, _jsonSerializer, config.TmdbApiKey);

                // --- 关键修订：区分详情ID和图像ID ---
                // imagesTmdbId 可能是 "12345" (Movie), "1396" (Series), 或 "1396_S1" (Season)
                var imagesTmdbId = GetTmdbId(item);
                if (string.IsNullOrEmpty(imagesTmdbId))
                {
                    _logger?.Debug("[OriginalPoster] No TMDB ID found for item, skipping");
                    return images;
                }

                string detailsTmdbId;
                bool isMovie = item is Movie; // Movie=true, Series=false, Season=false

                if (item is Season)
                {
                    // 播出季：详情ID是 Series ID (例如 "1396")
                    detailsTmdbId = imagesTmdbId.Split('_')[0];
                }
                else
                {
                    // 电影或剧集：详情ID和图像ID是相同的
                    detailsTmdbId = imagesTmdbId;
                }
                // --- 修订结束 ---


                // 1. 获取项目详情以确定原产国
                var details = await tmdbClient.GetItemDetailsAsync(detailsTmdbId, item is Movie, cancellationToken);

                string targetLanguage = "en";

                // 1. 优先 origin_country
                if (details?.origin_country?.Length > 0)
                {
                    var originCountry = details.origin_country[0];
                    targetLanguage = LanguageMapper.GetLanguageForCountry(originCountry);
                }
                // 2. 其次 original_language + production_countries 联合推断
                else if (!string.IsNullOrEmpty(details?.original_language) && details?.production_countries?.Length > 0)
                {
                    var originalLang = details.original_language;
                    // 尝试在 production_countries 中找匹配该语言的国家
                    var matchingCountry = details.production_countries
                        .FirstOrDefault(c => LanguageMapper.GetLanguageForCountry(c.iso_3166_1) == originalLang);
                
                    if (matchingCountry != null)
                    {
                        targetLanguage = originalLang; // 语言一致，可信
                    }
                    else
                    {
                        // 无匹配国家，仍使用 original_language（如 en 但国家是 JP，极少情况）
                        targetLanguage = originalLang;
                    }
                }
                // 3. 再次兜底 production_countries[0]
                else if (details?.production_countries?.Length > 0)
                {
                    var fallbackCountry = details.production_countries[0].iso_3166_1;
                    targetLanguage = LanguageMapper.GetLanguageForCountry(fallbackCountry);
                }
                // 4. 最终兜底 "en"
                // === 自动语言识别 ===

                // 2. 获取该语言的海报
                _logger?.Debug("[OriginalPoster] Fetching images for TMDB ID: {0}, language: {1}", tmdbId, targetLanguage);
                var result = await tmdbClient.GetImagesAsync(tmdbId, item is Movie, targetLanguage, cancellationToken);

                var allImages = new List<RemoteImageInfo>();

                // 添加海报
                if (result.posters != null)
                {
                    allImages.AddRange(ConvertToRemoteImageInfo(
                        result.posters, targetLanguage, config.MetadataLanguage, 
                        config.PosterSelectionStrategy, ImageType.Primary));
                }

                // 添加 Logo
                if (result.logos != null && config.EnableOriginalLogo)
                {
                    allImages.AddRange(ConvertToRemoteImageInfo(
                        result.logos, targetLanguage, config.MetadataLanguage, 
                        config.PosterSelectionStrategy, ImageType.Logo));
                }
                
                _logger?.Debug("[OriginalPoster] Fetched {0} images from TMDB", allImages.Count());
                
                return allImages;

//                if (imageType == ImageType.Primary)
//                {
//                    return ConvertToRemoteImageInfo(result.posters, targetLanguage, config.MetadataLanguage, config.PosterSelectionStrategy, ImageType.Primary);
//                }
//                else if (imageType == ImageType.Logo)
//                {
//                    return ConvertToRemoteImageInfo(result.logos, targetLanguage, config.MetadataLanguage, config.PosterSelectionStrategy, ImageType.Logo);
//                }
//            
//                return Enumerable.Empty<RemoteImageInfo>();

//                images = ConvertToRemoteImageInfo(result, targetLanguage, config.PosterSelectionStrategy);
//                _logger?.Debug("[OriginalPoster] Fetched {0} images from TMDB", allImages.Count());
            }
            catch (Exception ex)
            {
                //_logger?.Error(ex, "[OriginalPoster] Failed to fetch images from TMDB for {0}", item.Name);
                _logger?.Error("[OriginalPoster] Failed to fetch images from TMDB for {0}. Error: {1}", item.Name, ex.Message);
            }

            return images;
        }

//        private string GetTmdbId(BaseItem item)
//        {
//            if (item.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var id) == true)
//            {
//                return id;
//            }
//            return null;
//        }

        private string GetTmdbId(BaseItem item)
        {
            // 电影或剧集：直接从 ProviderIds 获取
            if (item is Movie || item is Series)
            {
                if (item.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var id) == true)
                {
                    return id;
                }
            }
            // 播出季：从 Parent Series 获取 TMDB ID + 季号
            else if (item is Season season)
            {
                var series = season.Series;
                if (series?.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var seriesTmdbId) == true)
                {
                    // 返回组合 ID，如 "1396_S1"
                    return $"{seriesTmdbId}_S{season.IndexNumber}";
                }
            }
            return null;
        }

		// 替换 OriginalPosterProvider.cs 中现有的 ConvertToRemoteImageInfo 方法
		private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(
		    TmdbImage[] images,
		    string targetLanguage,
		    string MetadataLanguage,
		    PosterSelectionStrategy strategy,
		    ImageType imageType)
		{
		    //if (tmdbResult?.posters == null || tmdbResult.posters.Length == 0)
		    if (images == null || images.Length == 0)
		        return Array.Empty<RemoteImageInfo>();
		
    		var config = Plugin.Instance?.Configuration;
		
		    // 1. 计算每个海报的“策略性”最终评分
		    //var candidates = tmdbResult.posters
		    var candidates = images
		       .Where(p =>!string.IsNullOrEmpty(p.file_path))
		       .Select(poster => new
		        {
		            Poster = poster,
		            OriginalLang = poster.iso_639_1,
		            DisplayLang = poster.iso_639_1?? targetLanguage,
		            CalculatedRating = GetStrategyBasedRating(poster, targetLanguage, strategy)
		        });
		
		    // 2. 仅按“最终评分”排序
		    // (您原来的 switch 排序块不再需要)
		    var sorted = candidates
		       .OrderByDescending(x => x.CalculatedRating)
		       .ThenByDescending(x => x.Poster.vote_count); // 使用 vote_count 作为决胜局
		
		    // 3. 将排序后的列表转换为 RemoteImageInfo
		    var result = sorted.Select(x => new RemoteImageInfo
		    {
		        ProviderName = Name,
//		        Type = ImageType.Primary,
		        Type = imageType, // ✅ 动态设置类型（Primary 或 Logo）
		        Url = $"https://image.tmdb.org/t/p/original{x.Poster.file_path}",
		        ThumbnailUrl = $"https://image.tmdb.org/t/p/w500{x.Poster.file_path}",
                Language = string.IsNullOrEmpty(MetadataLanguage) 
                    ? x.DisplayLang 
                    : MetadataLanguage, // 强制使用元数据语言
		        DisplayLanguage = GetDisplayLanguage(x.DisplayLang),
    	        Width = x.Poster.width,
		        Height = x.Poster.height,
		        CommunityRating = x.CalculatedRating,    // 分配我们预先计算好的、反映了策略的评分
		        VoteCount = x.Poster.vote_count,
		        RatingType = RatingType.Score
		    }).ToList();
		
		    // 您的日志记录（保持不变）
		    var top3 = result.Take(3).ToList();
		    for (int i = 0; i < top3.Count; i++)
		    {
		        var img = top3[i];
		        _logger?.Debug("[OriginalPoster] Returned image #{0}: URL={1}, Language={2}, Rating={3}",
		            i + 1, img.Url, img.Language, img.CommunityRating);
		    }
		    
//            // 关键：如果配置了 MetadataLanguage 且不等于 targetLanguage，额外返回一张“伪装成元数据语言”的海报
//            if (!string.IsNullOrEmpty(metadataLanguage) && 
//                !string.Equals(metadataLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
//            {
//                // 克隆最高评分的原语言海报，但 Language 设为 metadataLanguage
//                var topImage = result.FirstOrDefault();
//                if (topImage != null)
//                {
//                    var compatibleImage = new RemoteImageInfo
//                    {
//                        ProviderName = Name,
//                        Type = ImageType.Primary,
//                        Url = topImage.Url,
//                        ThumbnailUrl = topImage.ThumbnailUrl,
//                        Language = metadataLanguage, // 伪装成用户设置的语言
//                        DisplayLanguage = GetDisplayLanguage(metadataLanguage),
//                        Width = topImage.Width,
//                        Height = topImage.Height,
//                        CommunityRating = topImage.CommunityRating + 5, // 略高一点确保被选
//                        VoteCount = topImage.VoteCount,
//                        RatingType = RatingType.Score
//                    };
//                    result.Insert(0, compatibleImage); // 插入最前面
//                }
//            }
		
		    return result;
		}

        /// <summary>
        /// 根据用户策略计算海报的最终评分（基础分 + 奖励分）
        /// </summary>
        private double GetStrategyBasedRating(
            TmdbImage poster,                  // 只需传入完整对象
            string targetLanguage,             // 目标语言（用于判断是否“原语言”）
            PosterSelectionStrategy strategy)  // 策略
        {
            if (poster == null) return 0;
        
            double baseRating = poster.vote_average;
            string originalLang = poster.iso_639_1; // 内部直接读取
        
            switch (strategy)
            {
                case PosterSelectionStrategy.OriginalLanguageFirst:
                    if (originalLang == targetLanguage) return baseRating + 20;
                    if (originalLang == null) return baseRating + 10;
                    return baseRating;
        
                case PosterSelectionStrategy.NoTextPosterFirst:
                    if (originalLang == null) return baseRating + 20;
                    if (originalLang == targetLanguage) return baseRating + 10;
                    return baseRating;
        
                case PosterSelectionStrategy.HighestRatingFirst:
                default:
                    return baseRating;
            }
        }
        
        private string GetDisplayLanguage(string langCode)
        {
            // 1. 检查输入 (你的检查很好)
            if (string.IsNullOrWhiteSpace(langCode))
            {
                return "Unknown";
            }
        
            try
            {
                // 2. 尝试直接构造 (这已经处理了 "en", "ja", "zh" 等所有情况)
                CultureInfo ci = new CultureInfo(langCode);
                return ci.EnglishName;
            }
            catch (CultureNotFoundException)
            {
                 // （可选）在你的Emby插件日志中记录这个未知的代码
                // Logger.LogWarning($"[MyPlugin] Found unknown language code from TMDB: {langCode}");
                
                return langCode.ToUpperInvariant();
            }
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
//            return new[] { ImageType.Primary };
            return new[] { ImageType.Primary, ImageType.Logo }; // ✅ 添加 Logo
        }

        public bool Supports(BaseItem item, ImageType imageType)
        {
//            return imageType == ImageType.Primary && Supports(item);
            return (imageType == ImageType.Primary || imageType == ImageType.Logo)
                    && Supports(item);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            // 记录 Emby 实际请求的图片（即最终选中的）
            _logger?.Info("[OriginalPoster] Emby selected image for download: {0}", url);
            
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }
    }
}
