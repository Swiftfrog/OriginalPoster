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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using OriginalPoster.Models;
using OriginalPoster.Services;

namespace OriginalPoster.Providers;

public class OriginalPosterProvider : IRemoteImageProvider, IHasOrder
{
    private readonly IHttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly IJsonSerializer _jsonSerializer;

    public string Name => "OriginalPoster";
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
        var supported = item is Movie || item is Series || item is Season|| item is BoxSet;
        _logger?.Debug("[OriginalPoster] Supports check for {0}: {1}", item.Name ?? "Unknown", supported);
        return supported;
    }

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        
        // 检查空配置
        if (config == null)
        {
            _logger?.Debug("[OriginalPoster] Configuration not found. Plugin disabled.");
            return Enumerable.Empty<RemoteImageInfo>();
        }
        
        // 全局开关：插件是否启用
        if (config.Enabled != true)
        {
            _logger?.Debug("[OriginalPoster] Please Enable Plugin");
            return Enumerable.Empty<RemoteImageInfo>();
        }
        
        _logger?.Debug("[OriginalPoster] GetImages called for: {0}", item.Name);

        var images = Enumerable.Empty<RemoteImageInfo>();

        /// 测试模式
        if (config.TestMode == true)
        {
            _logger?.Debug("[OriginalPoster] Test mode enabled, returning test poster");
            
            string testlangCode = !string.IsNullOrWhiteSpace(libraryOptions.PreferredMetadataLanguage) 
                ? libraryOptions.PreferredMetadataLanguage.Trim()
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
        /// 测试模式

        var tmdbId = GetTmdbId(item);
        
        if (string.IsNullOrEmpty(tmdbId))
        {
            _logger?.Debug("[OriginalPoster] No TMDB ID found for item, skipping");
            return images;
        }

        try
        {
            // 检查TMDB API Key 不能为空
            if (string.IsNullOrEmpty(config.TmdbApiKey))
            {
                _logger?.Debug("[OriginalPoster] Please fill in TMDB API KEY.");
                return Enumerable.Empty<RemoteImageInfo>();
            }
            
            var tmdbClient = new TmdbClient(_httpClient, _jsonSerializer, config.TmdbApiKey);

            // 区分详情ID和图像ID
            // imagesTmdbId 可能是 "12345" (Movie), "1396" (Series), 或 "1396_S1" (Season)
            var imagesTmdbId = GetTmdbId(item);

            if (string.IsNullOrEmpty(imagesTmdbId))
            {
                _logger?.Debug("[OriginalPoster] No TMDB ID found for item, skipping");
                return Enumerable.Empty<RemoteImageInfo>(); // 直接返回
            }

            string detailsTmdbId;
            // bool isMovie = item is Movie; // Movie=true, Series=false, Season=false
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
            
            string detailsType = item switch    // 把emby的item属性转为TMDB的属性
            {
                Movie => "movie",
                Series => "tv",
                Season => "tv", 
                BoxSet => "collection",
                _ => "movie" // 对于其他未知类型，默认使用movie API（相对更通用）
            };

            var details = await tmdbClient.GetItemDetailsAsync(detailsTmdbId, detailsType, cancellationToken);

            string targetLanguage = "en";    //设置fallback为en
            
            // [修改开始] ----------------------------------------------------
            // 1. 尝试从持久化缓存读取
            if (Plugin.Instance?.CacheManager.TryGetLanguage(detailsTmdbId, detailsType, out var cachedLang) == true)
            {
                _logger?.Debug("[OriginalPoster] Cache HIT for {0}: {1}", detailsTmdbId, cachedLang);
                targetLanguage = cachedLang!;
            }
            else
            {
                // 2. 缓存未命中，请求 API
                _logger?.Debug("[OriginalPoster] Cache MISS for {0}, fetching from TMDB...", detailsTmdbId);
                
                var details = await tmdbClient.GetItemDetailsAsync(detailsTmdbId, detailsType, cancellationToken);
                        
                if (details != null)
                {
                    string originalLang = details.original_language;
                    if (originalLang == "cn") originalLang = "zh"; // 标准化
                
                    // 只要 original_language 存在，就优先使用它,不区分中文CN，HK,TW和SG
                    if (!string.IsNullOrEmpty(originalLang))
                    {
                        targetLanguage = originalLang;
                    }
                    // 否则回退到 origin_country 或 production_countries
                    else if (details.origin_country?.Length > 0)
                    {
                        targetLanguage = LanguageMapper.GetLanguageForCountry(details.origin_country[0]);
                    }
                    else if (details.production_countries?.Length > 0)
                    {
                        targetLanguage = LanguageMapper.GetLanguageForCountry(details.production_countries[0].iso_3166_1);
                    }
                    
                    Plugin.Instance?.CacheManager.AddAndSave(detailsTmdbId, detailsType, targetLanguage);
                    
                }                
            }
            
            // if (details != null)
            // {
            //     string originalLang = details.original_language;
            //     if (originalLang == "cn") originalLang = "zh"; // 标准化
            // 
            //     // ✅ 1. 先快速处理非中文（占大多数）
            //     if (!string.IsNullOrEmpty(originalLang) && originalLang != "zh")
            //     {
            //         targetLanguage = originalLang; // 直接返回，无需查 country
            //     }
            //     // ✅ 2. 再处理中文（少数情况）
            //     else if (originalLang == "zh" && details.origin_country?.Length > 0)
            //     {
            //         var zhPriority = new[] { "HK", "TW", "SG", "CN" };
            //         var primaryCountry = details.origin_country
            //             .FirstOrDefault(c => zhPriority.Contains(c)) 
            //             ?? details.origin_country[0];
            //         targetLanguage = LanguageMapper.GetLanguageForCountry(primaryCountry);
            //     }
            //     // ✅ 3. 最后处理无 original_language 的情况
            //     else if (details.origin_country?.Length > 0)
            //     {
            //         targetLanguage = LanguageMapper.GetLanguageForCountry(details.origin_country[0]);
            //     }
            //     else if (details.production_countries?.Length > 0)
            //     {
            //         targetLanguage = LanguageMapper.GetLanguageForCountry(details.production_countries[0].iso_3166_1);
            //     }
            // }
            
            _logger?.Debug(
                "[OriginalPoster] Detected: original_language={0}, origin_country=[{1}], production_countries=[{2}] → targetLanguage={3}",
                details?.original_language,
                string.Join(",", details?.origin_country ?? Array.Empty<string>()),
                string.Join(",", details?.production_countries?.Select(p => p.iso_3166_1) ?? Array.Empty<string>()),
                targetLanguage);
                
            // 获取该语言的海报
            _logger?.Debug("[OriginalPoster] Fetching images for TMDB ID: {0}, language: {1}", imagesTmdbId, targetLanguage);
            
            // 获取图像 - 根据项目类型选择正确的API端点
            string imagesType = item switch
            {
                Movie => "movie",
                Series => "tv",
                Season => "tv_season", 
                BoxSet => "collection",
                _ => "movie" // 对于其他未知类型，默认使用movie API（相对更通用）
            };
            
            var result = await tmdbClient.GetImagesAsync(imagesTmdbId, imagesType, targetLanguage, cancellationToken);

            var allImages = new List<RemoteImageInfo>();

            // 海报-movie, series, season
            if (result.posters != null)
            {
                allImages.AddRange(ConvertToRemoteImageInfo(
                    result.posters, targetLanguage, libraryOptions, 
                    config.PosterSelectionStrategy, ImageType.Primary));
            }

            // Logo
            if (result.logos != null && config.EnableOriginalLogo)
            {
                allImages.AddRange(ConvertToRemoteImageInfo(
                    result.logos, targetLanguage, libraryOptions, 
                    config.PosterSelectionStrategy, ImageType.Logo));
            }
            
            _logger?.Debug("[OriginalPoster] Fetched {0} images from TMDB", allImages.Count());
            
            return allImages;

        }
        catch (Exception ex)
        {
            _logger?.Error("[OriginalPoster] Failed to fetch images from TMDB for {0}. Error: {1}", item.Name, ex.Message);
        }

        return images;
    }

    private string? GetTmdbId(BaseItem item)
    {
        // ✅ 1. 优先处理 BoxSet（专用逻辑）
        if (item is BoxSet boxSet)
        {
            string? idString = null;
            
            // 优先尝试专用 Key "TmdbCollection"
            if (boxSet.ProviderIds?.TryGetValue("TmdbCollection", out idString) != true)
            {
                // 回退到标准 Key "Tmdb"
                boxSet.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out idString);
            }
            
            if (!string.IsNullOrEmpty(idString))
            {
                // 清理前缀
                if (idString.StartsWith("collection/", StringComparison.OrdinalIgnoreCase))
                {
                    idString = idString.Substring("collection/".Length);
                }
                
                // 验证是否为数字
                if (int.TryParse(idString, out _))
                {
                    _logger?.Debug("[OriginalPoster] Found BoxSet TMDB ID: {0}", idString);
                    return idString;
                }
            }
            
            _logger?.Debug("[OriginalPoster] BoxSet found, but no valid numeric TMDB ID in ProviderIds.");
            return null;
        }
        
        // ✅ 2. 处理 Movie/Series（简单逻辑）
        if (item is Movie || item is Series)
        {
            if (item.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var id) == true)
            {
                _logger?.Debug("[OriginalPoster] TMDB ID: {0}", id);
                return id;
            }
        }
        
        // ✅ 3. 处理 Season
        else if (item is Season season)
        {
            var series = season.Series;
            if (series?.ProviderIds?.TryGetValue(MetadataProviders.Tmdb.ToString(), out var seriesTmdbId) == true)
            {
                var seasonId = $"{seriesTmdbId}_S{season.IndexNumber}";
                _logger?.Debug("[OriginalPoster] Season composite TMDB ID: {0}", seasonId);
                return seasonId;
            }
        }
        
        return null;
    }

	// 替换 OriginalPosterProvider.cs 中现有的 ConvertToRemoteImageInfo 方法
	private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(
	    TmdbImage[] images,
	    string targetLanguage,
	    LibraryOptions libraryOptions,
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
	        Type = imageType, // 动态设置类型（Primary 或 Logo）
	        Url = $"https://image.tmdb.org/t/p/original{x.Poster.file_path}",
	        ThumbnailUrl = $"https://image.tmdb.org/t/p/w500{x.Poster.file_path}",
            Language = !string.IsNullOrEmpty(libraryOptions.PreferredMetadataLanguage)    // 优先使用库语言，否则用原语言
                ? libraryOptions.PreferredMetadataLanguage
                : x.DisplayLang, 
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
        string targetLangBase = targetLanguage.Split('-')[0]; //提取 targetLanguage 的主语言部分（"zh-CN" → "zh"）
    
        switch (strategy)
        {
            case PosterSelectionStrategy.OriginalLanguageFirst:
                if (originalLang == targetLangBase) return baseRating + 20;
                if (originalLang == null) return baseRating + 10;
                return baseRating;
    
            case PosterSelectionStrategy.NoTextPosterFirst:
                if (originalLang == null) return baseRating + 20;
                if (originalLang == targetLangBase) return baseRating + 10;
                return baseRating;
    
            case PosterSelectionStrategy.HighestRatingFirst:
            default:
                return baseRating;
        }
    }
    
    private string GetDisplayLanguage(string langCode)
    {
        // 检查输入
        if (string.IsNullOrWhiteSpace(langCode))
        {
            return "Unknown";
        }
    
        try
        {
            // 尝试直接构造 (这已经处理了 "en", "ja", "zh" 等所有情况)
            CultureInfo ci = new CultureInfo(langCode);
            return ci.EnglishName;
        }
        catch (CultureNotFoundException)
        {
            return langCode.ToUpperInvariant();
        }
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[] { ImageType.Primary, ImageType.Logo }; // Movie, Series, Season, Logo
    }

    public bool Supports(BaseItem item, ImageType imageType)
    {
        return (imageType == ImageType.Primary || imageType == ImageType.Logo) && Supports(item);
    }

    public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        _logger?.Info("[OriginalPoster] Emby selected image for download: {0}", url);    // 记录 Emby 实际请求的图片（即最终选中的）
        
        return _httpClient.GetResponse(new HttpRequestOptions
        {
            Url = url,
            CancellationToken = cancellationToken
        });
    }
}
