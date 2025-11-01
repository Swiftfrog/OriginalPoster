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
                string targetLanguage = "en"; // 默认英语

                if (details?.production_countries?.Length > 0)
                {
                    var primaryCountry = details.production_countries[0].iso_3166_1;
                    targetLanguage = LanguageMapper.GetLanguageForCountry(primaryCountry);
                    _logger?.Debug("[OriginalPoster] Primary country: {0}, mapped language: {1}", primaryCountry, targetLanguage);
                }

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
		
		    // 1. 计算每个海报的“策略性”最终评分
		    var candidates = tmdbResult.posters
		       .Where(p =>!string.IsNullOrEmpty(p.file_path))
		       .Select(poster => new
		        {
		            Poster = poster,
		            OriginalLang = poster.iso_639_1,
		            DisplayLang = poster.iso_639_1?? targetLanguage,
		            // 核心修复：在这里预先计算最终评分
		            CalculatedRating = GetStrategyBasedRating(poster, poster.iso_639_1, targetLanguage, strategy)
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
		        DisplayLanguage = GetDisplayLanguage(x.DisplayLang),
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
		
		    return result;
		}

		// 将这个新方法添加到您的 OriginalPosterProvider.cs 类中
		// (假设 TmdbPoster 定义在 OriginalPoster.Models 命名空间下)
		/// <summary>
		/// 根据用户策略计算海报的最终评分（基础分 + 奖励分）
		/// </summary>
		private double GetStrategyBasedRating(OriginalPoster.Models.TmdbImage poster, string originalLang, string targetLanguage, PosterSelectionStrategy strategy)
		{
		    double baseRating = poster.vote_average;
		
		    switch (strategy)
		    {
		        case PosterSelectionStrategy.OriginalLanguageFirst:
		            if (originalLang == targetLanguage) return baseRating + 20; // 原语言 +20
		            if (originalLang == null) return baseRating + 10;           // 无文字 +10
		            return baseRating; // 其他语言
		
		        case PosterSelectionStrategy.NoTextPosterFirst:
		            if (originalLang == null) return baseRating + 20;           // 无文字 +20
		            if (originalLang == targetLanguage) return baseRating + 10; // 原语言 +10
		            return baseRating; // 其他语言
		
		        case PosterSelectionStrategy.HighestRatingFirst:
		        default:
		            // 即使是“最高评分”，我们仍然需要为我们的候选海报（原语言和无文字）
		            // 增加一个适度的奖励，以确保它们能战胜来自Emby默认TMDB供应的相同海报。
		            if (originalLang == targetLanguage || originalLang == null) return baseRating + 10;
		            return baseRating; // 其他语言不加分
		    }
		}

//        private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(
//            TmdbImageResult tmdbResult,
//            string targetLanguage,
//            PosterSelectionStrategy strategy)
//        {
//            if (tmdbResult?.posters == null || tmdbResult.posters.Length == 0)
//                return Array.Empty<RemoteImageInfo>();
//        
//            // 构建候选列表，保留原始语言信息
//            var candidates = tmdbResult.posters
//                .Where(p => !string.IsNullOrEmpty(p.file_path))
//                .Select(poster => new
//                {
//                    Poster = poster,
//                    OriginalLang = poster.iso_639_1,           // 原始语言（可能为 null）
//                    DisplayLang = poster.iso_639_1 ?? targetLanguage // 显示语言
//                })
//                .ToList();
//        
//            IOrderedEnumerable<dynamic> sorted;
//        
//            switch (strategy)
//            {
//                case PosterSelectionStrategy.OriginalLanguageFirst:
//                    sorted = candidates
//                        .OrderByDescending(x =>
//                        {
//                            if (x.OriginalLang == targetLanguage) return 3; // 原生语言
//                            if (x.OriginalLang == null) return 2;           // 无文字
//                            return 1;                                       // 其他语言
//                        })
//                        .ThenByDescending(x => x.Poster.vote_average);
//                    break;
//        
//                case PosterSelectionStrategy.HighestRatingFirst:
//                    sorted = candidates.OrderByDescending(x => x.Poster.vote_average);
//                    break;
//        
//                case PosterSelectionStrategy.NoTextPosterFirst:
//                    sorted = candidates
//                        .OrderByDescending(x =>
//                        {
//                            if (x.OriginalLang == null) return 3;           // 无文字最高
//                            if (x.OriginalLang == targetLanguage) return 2; // 原生语言次之
//                            return 1;                                       // 其他语言最后
//                        })
//                        .ThenByDescending(x => x.Poster.vote_average);
//                    break;
//        
//                default:
//                    sorted = candidates.OrderByDescending(x => x.Poster.vote_average);
//                    break;
//            }
//            
//            var result = sorted.Select(x => new RemoteImageInfo
//            {
//                ProviderName = Name,
//                Type = ImageType.Primary,
//                Url = $"https://image.tmdb.org/t/p/original{x.Poster.file_path}",
//                ThumbnailUrl = $"https://image.tmdb.org/t/p/w500{x.Poster.file_path}",
//                Language = x.DisplayLang,
//                DisplayLanguage = GetDisplayLanguage(x.DisplayLang),
//                Width = x.Poster.width,
//                Height = x.Poster.height,
//                // 给我需要的海报评分 +10，确保 Emby 优先选择你的结果，只给原语言和无文字海报加分，优先评分的不需要在分数上干预。
//                CommunityRating = x.OriginalLang == targetLanguage
//                    ? x.Poster.vote_average + 20
//                    : x.OriginalLang == null
//                        ? x.Poster.vote_average + 10
//                        : x.Poster.vote_average,
//                VoteCount = x.Poster.vote_count,
//                RatingType = RatingType.Score
//            }).ToList(); // 转为 List 以便取前几项
//        
//            // ✅ 新增日志：记录返回的前3张图片
//            var top3 = result.Take(3).ToList();
//            for (int i = 0; i < top3.Count; i++)
//            {
//                var img = top3[i];
//                _logger?.Info("[OriginalPoster] Returned image #{0}: URL={1}, Language={2}, Rating={3}",
//                    i + 1, img.Url, img.Language, img.CommunityRating);
//            }
//        
//            return result; 
//        
//        }

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
