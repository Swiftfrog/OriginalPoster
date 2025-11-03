// Providers/Originalposterprovider.cs
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster.Providers
{
    /// <summary>
    /// 原语言海报提供者 - 第三阶段：支持自动识别原语言
    /// </summary>
    public class OriginalPosterProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public string Name => "TMDB Original Language";
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

            var images = Enumerable.Empty<RemoteImageInfo>();

            // === 第一阶段：测试模式 ===
            if (config?.TestMode == true)
            {
                _logger?.Debug("[OriginalPoster] Test mode enabled, returning test poster");
                var testImage = new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = config.TestPosterUrl.Trim(),
                    ThumbnailUrl = config.TestPosterUrl.Trim(),
                    Language = "zh",
                    DisplayLanguage = "Korean",
                    Width = 1000,
                    Height = 1500,
                    CommunityRating = 10,
                    VoteCount = 100,
                    RatingType = RatingType.Score
                };
                _logger?.Debug("[OriginalPoster] Returning 1 test image");
                return new[] { testImage };
            }

            // === 第三阶段：自动语言识别 ===
            var tmdbId = GetTmdbId(item);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger?.Debug("[OriginalPoster] No TMDB ID found for item, skipping");
                return images;
            }

            try
            {
                var tmdbClient = new TmdbClient(_httpClient, _jsonSerializer, config.TmdbApiKey);

                // 1. 获取项目详情以确定原产国
                var details = await tmdbClient.GetItemDetailsAsync(tmdbId, item is Movie, cancellationToken);
//                string targetLanguage = "en"; // 默认英语
//
//                // 1. 优先 original_language
//                if (!string.IsNullOrEmpty(details?.original_language))
//                {
//                    targetLanguage = details.original_language;
//                }
//                // 2. 其次 origin_country[0]
//                else if (details?.origin_country?.Length > 0)
//                {
//                    var originCountry = details.origin_country[0];
//                    targetLanguage = LanguageMapper.GetLanguageForCountry(originCountry);
//                }

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


                // 2. 获取该语言的海报
                _logger?.Debug("[OriginalPoster] Fetching images for TMDB ID: {0}, language: {1}", tmdbId, targetLanguage);
                var result = await tmdbClient.GetImagesAsync(tmdbId, item is Movie, targetLanguage, cancellationToken);

                //images = ConvertToRemoteImageInfo(result, targetLanguage);
                images = ConvertToRemoteImageInfo(result, targetLanguage, config.PosterSelectionStrategy);
                _logger?.Debug("[OriginalPoster] Fetched {0} images from TMDB", images.Count());
            }
            catch (Exception ex)
            {
                //_logger?.Error(ex, "[OriginalPoster] Failed to fetch images from TMDB for {0}", item.Name);
                _logger?.Error("[OriginalPoster] Failed to fetch images from TMDB for {0}. Error: {1}", item.Name, ex.Message);
            }

            return images;
        }

        private string GetTmdbId(BaseItem item)
        {
            if (item.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var id) == true)
            {
                return id;
            }
            return null;
        }

		// 替换 OriginalPosterProvider.cs 中现有的 ConvertToRemoteImageInfo 方法
		private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(
		    TmdbImageResult tmdbResult,
		    string targetLanguage,
		    PosterSelectionStrategy strategy)
		{
		    if (tmdbResult?.posters == null || tmdbResult.posters.Length == 0)
		        return Array.Empty<RemoteImageInfo>();
		
    		var config = Plugin.Instance?.Configuration;
		
		    // 1. 计算每个海报的“策略性”最终评分
		    var candidates = tmdbResult.posters
		       .Where(p =>!string.IsNullOrEmpty(p.file_path))
		       .Select(poster => new
		        {
		            Poster = poster,
		            OriginalLang = poster.iso_639_1,
		            DisplayLang = poster.iso_639_1?? targetLanguage,
		            // 核心修复：在这里预先计算最终评分
		            // CalculatedRating = GetStrategyBasedRating(poster, poster.iso_639_1, targetLanguage, strategy)
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
		        Type = ImageType.Primary,
		        Url = $"https://image.tmdb.org/t/p/original{x.Poster.file_path}",
		        ThumbnailUrl = $"https://image.tmdb.org/t/p/w500{x.Poster.file_path}",
		        Language = x.DisplayLang,
		        // DisplayLanguage = GetDisplayLanguage(x.DisplayLang),
                DisplayLanguage = string.IsNullOrEmpty(config?.DisplayLanguageOverride)
                    ? GetDisplayLanguage(x.DisplayLang)   // 默认
                    : config.DisplayLanguageOverride,     // ✅ 强制覆盖（如 "Chinese"）		        
		        Width = x.Poster.width,
		        Height = x.Poster.height,
		        
		        // 分配我们预先计算好的、反映了策略的评分
		        CommunityRating = x.CalculatedRating,
		                                    
		        VoteCount = x.Poster.vote_count,
		        RatingType = RatingType.Score
		    }).ToList();
		
		    // 您的日志记录（保持不变）
		    var top3 = result.Take(3).ToList();
		    for (int i = 0; i < top3.Count; i++)
		    {
		        var img = top3[i];
		        _logger?.Info("[OriginalPoster] Returned image #{0}: URL={1}, Language={2}, Rating={3}",
		            i + 1, img.Url, img.Language, img.CommunityRating);
		    }
		    
//            // ✅ 关键：如果配置了 MetadataLanguage 且不等于 targetLanguage，额外返回一张“伪装成元数据语言”的海报
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
//                        Language = metadataLanguage, // 👈 伪装成用户设置的语言
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

		// 将这个新方法添加到您的 OriginalPosterProvider.cs 类中
		// (假设 TmdbPoster 定义在 OriginalPoster.Models 命名空间下)
		/// <summary>
		/// 根据用户策略计算海报的最终评分（基础分 + 奖励分）
		/// </summary>
        //		private double GetStrategyBasedRating(TmdbImage poster, string originalLang, string targetLanguage, PosterSelectionStrategy strategy)
        //		{
        //		    double baseRating = poster.vote_average;
        //		
        //		    switch (strategy)
        //		    {
        //		        case PosterSelectionStrategy.OriginalLanguageFirst:
        //		            if (originalLang == targetLanguage) return baseRating + 20; // 原语言 +20
        //		            if (originalLang == null) return baseRating + 10;           // 无文字 +10
        //		            return baseRating; // 其他语言
        //		
        //		        case PosterSelectionStrategy.NoTextPosterFirst:
        //		            if (originalLang == null) return baseRating + 20;           // 无文字 +20
        //		            if (originalLang == targetLanguage) return baseRating + 10; // 原语言 +10
        //		            return baseRating; // 其他语言
        //		
        //		        case PosterSelectionStrategy.HighestRatingFirst:
        //		        default:
        //		            // 即使是“最高评分”，我们仍然需要为我们的候选海报（原语言和无文字）
        //		            // 增加一个适度的奖励，以确保它们能战胜来自Emby默认TMDB供应的相同海报。
        //		            // if (originalLang == targetLanguage || originalLang == null) return baseRating + 10;
        //		            return baseRating; // 其他语言不加分
        //		    }
        //		}

        /// <summary>
        /// 根据用户策略计算海报的最终评分（基础分 + 奖励分）
        /// </summary>
        private double GetStrategyBasedRating(
            TmdbImage poster,                  // ✅ 只需传入完整对象
            string targetLanguage,             // 目标语言（用于判断是否“原语言”）
            PosterSelectionStrategy strategy)  // 策略
        {
            if (poster == null) return 0;
        
            double baseRating = poster.vote_average;
            string originalLang = poster.iso_639_1; // ✅ 内部直接读取
        
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
            return langCode switch
            {
                "en" => "English",
                "zh" => "Chinese",
                "ja" => "Japanese",
                "ko" => "Korean",
                "fr" => "French",
                "de" => "German",
                "es" => "Spanish",
                "it" => "Italian",
                "ru" => "Russian",
                "ar" => "Arabic",
                "hi" => "Hindi",
                "th" => "Thai",
                "pt" => "Portuguese",
                "nl" => "Dutch",
                "sv" => "Swedish",
                "no" => "Norwegian",
                "da" => "Danish",
                "fi" => "Finnish",
                "pl" => "Polish",
                "cs" => "Czech",
                "hu" => "Hungarian",
                "el" => "Greek",
                "tr" => "Turkish",
                "he" => "Hebrew",
                "fa" => "Persian",
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
            // ✅ 新增日志：记录 Emby 实际请求的图片（即最终选中的）
            _logger?.Info("[OriginalPoster] Emby selected image for download: {0}", url);
            
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }
    }
}
