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

                images = ConvertToRemoteImageInfo(result, targetLanguage);
                _logger?.Debug("[OriginalPoster] Fetched {0} images from TMDB", images.Count());
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "[OriginalPoster] Failed to fetch images from TMDB for {0}", item.Name);
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

        private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(TmdbImageResult tmdbResult, string fallbackLanguage)
        {
            if (tmdbResult?.posters == null || tmdbResult.posters.Length == 0)
                return Array.Empty<RemoteImageInfo>();

            var list = new List<RemoteImageInfo>();
            foreach (var poster in tmdbResult.posters)
            {
                if (string.IsNullOrEmpty(poster.file_path))
                    continue;

                // 🔥 修复：删除 URL 中的多余空格！
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

            return list.OrderByDescending(img => img.CommunityRating ?? 0);
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
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }
    }
}