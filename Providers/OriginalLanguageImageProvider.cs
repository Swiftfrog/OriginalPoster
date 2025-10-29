using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace OriginalPoster.Providers
{
    public class OriginalLanguageImageProvider : IRemoteImageProvider, IHasOrder
    {
        public string Name => "OriginalPoster";
        
        // 关键：设置为负数，确保在 TMDB 之前执行
        public int Order => -100;

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public OriginalLanguageImageProvider(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _logger.Info("=== OriginalLanguageImageProvider constructed ===");
        }

        public bool Supports(BaseItem item)
        {
            var isMovie = item is Movie;
            var hasTmdbId = isMovie && item.HasProviderId(MetadataProviders.Tmdb);
            
            if (hasTmdbId)
            {
                _logger.Info("Supports TRUE for: {0} (TMDB ID: {1})", 
                    item.Name, 
                    item.GetProviderId(MetadataProviders.Tmdb));
            }
            
            return hasTmdbId;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            _logger.Info("╔════════════════════════════════════════════════════════════");
            _logger.Info("║ GetImages CALLED for: {0}", item.Name);
            _logger.Info("╚════════════════════════════════════════════════════════════");

            var movie = item as Movie;
            if (movie == null)
            {
                _logger.Warn("Not a Movie, returning empty list.");
                return new List<RemoteImageInfo>();
            }

            // 检查插件配置
            if (Plugin.Instance == null)
            {
                _logger.Error("Plugin.Instance is NULL!");
                return new List<RemoteImageInfo>();
            }

            var config = Plugin.Instance.PluginConfiguration;
            if (config == null)
            {
                _logger.Error("Configuration is NULL!");
                return new List<RemoteImageInfo>();
            }

            _logger.Info("Plugin Enabled: {0}", config.EnablePlugin);
            _logger.Info("API Key Configured: {0}", !string.IsNullOrEmpty(config.TmdbApiKey));

            if (!config.EnablePlugin)
            {
                _logger.Info("Plugin is disabled, returning empty list.");
                return new List<RemoteImageInfo>();
            }

            var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger.Warn("No TMDB ID for movie: {0}", movie.Name);
                return new List<RemoteImageInfo>();
            }

            var apiKey = config.TmdbApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Error("TMDB API Key not configured! Please set it in plugin settings.");
                return new List<RemoteImageInfo>();
            }

            _logger.Info("Processing movie: {0}, TMDB ID: {1}", movie.Name, tmdbId);

            try
            {
                // 1. 获取原始语言
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not determine original language for TMDB ID: {0}", tmdbId);
                    return new List<RemoteImageInfo>();
                }

                _logger.Info("Original language detected: {0}", originalLanguage);

                // 2. 获取并排序海报
                var sortedPosters = await GetSortedPostersAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                var originalCount = sortedPosters.Count(p => p.Language == originalLanguage);
                _logger.Info("SUCCESS: Returning {0} total posters ({1} original language, {2} others)", 
                    sortedPosters.Count, 
                    originalCount,
                    sortedPosters.Count - originalCount);

                // 即使只有1张海报也返回，让用户看到我们的 Provider 在工作
                return sortedPosters;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"ERROR processing movie '{movie.Name}' (TMDB: {tmdbId})", ex);
                return new List<RemoteImageInfo>();
            }
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.Debug("GetImageResponse called for URL: {0}", url);
            // Emby 会自己下载图片，这里返回空实现
            return Task.FromResult(new HttpResponseInfo());
        }

        private async Task<string> GetOriginalLanguageAsync(string tmdbId, string apiKey, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}&language=en-US";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableHttpCompression = true
            };

            _logger.Debug("Fetching movie details from: {0}", url.Replace(apiKey, "***"));

            try
            {
                using var response = await _httpClient.GetResponse(options);
                using var stream = response.Content;
                using var reader = new StreamReader(stream);
                var jsonString = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("original_language", out var langElement))
                {
                    var lang = langElement.GetString();
                    _logger.Debug("Found original language: {0}", lang);
                    return lang;
                }

                _logger.Warn("No 'original_language' property in TMDB response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Failed to fetch original language for TMDB ID: {tmdbId}", ex);
                return null;
            }
        }

        private async Task<List<RemoteImageInfo>> GetSortedPostersAsync(string tmdbId, string apiKey, string originalLanguage, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/images?api_key={apiKey}";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableHttpCompression = true
            };

            _logger.Debug("Fetching posters from: {0}", url.Replace(apiKey, "***"));

            try
            {
                using var response = await _httpClient.GetResponse(options);
                using var stream = response.Content;
                using var reader = new StreamReader(stream);
                var jsonString = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(jsonString);
                
                if (!doc.RootElement.TryGetProperty("posters", out var postersProperty))
                {
                    _logger.Warn("No 'posters' property in TMDB images response");
                    return new List<RemoteImageInfo>();
                }

                var postersArray = postersProperty.EnumerateArray();
                var matchingPosters = new List<RemoteImageInfo>();
                var otherPosters = new List<RemoteImageInfo>();

                foreach (var poster in postersArray)
                {
                    if (!poster.TryGetProperty("file_path", out var filePathProp))
                        continue;

                    var filePath = filePathProp.GetString();
                    if (string.IsNullOrEmpty(filePath))
                        continue;

                    var isoCode = poster.TryGetProperty("iso_639_1", out var isoProp) 
                        ? isoProp.GetString() 
                        : null;

                    var fullImageUrl = $"https://image.tmdb.org/t/p/original{filePath}";
                    
                    var imageInfo = new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = fullImageUrl,
                        Type = ImageType.Primary,
                        Language = isoCode
                    };

                    if (isoCode == originalLanguage)
                    {
                        matchingPosters.Add(imageInfo);
                        _logger.Debug("✓ Original language poster: {0}", filePath);
                    }
                    else
                    {
                        otherPosters.Add(imageInfo);
                        _logger.Debug("  Other language poster ({0}): {1}", isoCode ?? "null", filePath);
                    }
                }

                // 原语言海报优先
                var sortedList = new List<RemoteImageInfo>(matchingPosters);
                sortedList.AddRange(otherPosters);

                _logger.Info("Sorted {0} posters: {1} original language, {2} others", 
                    sortedList.Count, 
                    matchingPosters.Count, 
                    otherPosters.Count);

                return sortedList;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Failed to fetch posters for TMDB ID: {tmdbId}", ex);
                return new List<RemoteImageInfo>();
            }
        }
    }
}