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
    /// <summary>
    /// 原生语言海报优先 Provider
    /// </summary>
    public class OriginalLanguageImageProvider : IRemoteImageProvider, IHasOrder, IHasSupportedExternalIdentifiers
    {
        public string Name => "OriginalPoster";
        
        // 设置为负数，确保在 TMDB 之前执行
        public int Order => -100;

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        // Emby 会通过依赖注入自动调用这个构造函数
        public OriginalLanguageImageProvider(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _logger.Info("╔═══════════════════════════════════════════════════════════");
            _logger.Info("║ OriginalLanguageImageProvider initialized");
            _logger.Info("║ Name: {0}, Order: {1}", Name, Order);
            _logger.Info("╚═══════════════════════════════════════════════════════════");
        }

        public bool Supports(BaseItem item)
        {
            var isMovie = item is Movie;
            var hasTmdbId = isMovie && item.HasProviderId(MetadataProviders.Tmdb);
            
            // 强制日志，每次都输出
            _logger.Info("═══ Supports called for: {0} (IsMovie: {1}, HasTmdb: {2}) ═══", 
                item?.Name ?? "null", 
                isMovie, 
                hasTmdbId);
            
            if (hasTmdbId)
            {
                _logger.Info("→→→ Supports returning TRUE for: {0}", item.Name);
            }
            else
            {
                _logger.Debug("→→→ Supports returning FALSE for: {0}", item?.Name ?? "null");
            }
            
            return hasTmdbId;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            _logger.Info("╔═══════════════════════════════════════════════════════════");
            _logger.Info("║ GetImages called for: {0}", item.Name);
            _logger.Info("╚═══════════════════════════════════════════════════════════");

            var movie = item as Movie;
            if (movie == null)
            {
                _logger.Warn("Item is not a Movie.");
                return new List<RemoteImageInfo>();
            }

            // 检查插件配置
            if (Plugin.Instance?.PluginConfiguration == null)
            {
                _logger.Error("Plugin configuration not available!");
                return new List<RemoteImageInfo>();
            }

            var config = Plugin.Instance.PluginConfiguration;
            
            _logger.Info("Config - Enabled: {0}, API Key: {1}", 
                config.EnablePlugin, 
                !string.IsNullOrEmpty(config.TmdbApiKey) ? "Set" : "NOT SET");

            if (!config.EnablePlugin)
            {
                _logger.Info("Plugin is disabled in settings.");
                return new List<RemoteImageInfo>();
            }

            var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger.Warn("Movie has no TMDB ID.");
                return new List<RemoteImageInfo>();
            }

            var apiKey = config.TmdbApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Error("TMDB API Key not configured! Please configure in plugin settings.");
                _logger.Error("Go to: Emby Console → Plugins → OriginalPoster Settings");
                return new List<RemoteImageInfo>();
            }

            _logger.Info("Processing: {0} (TMDB: {1})", movie.Name, tmdbId);

            try
            {
                // 1. 获取原始语言
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not determine original language.");
                    return new List<RemoteImageInfo>();
                }

                _logger.Info("Original language: {0}", originalLanguage);

                // 2. 获取并排序海报
                var sortedPosters = await GetSortedPostersAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                var originalCount = sortedPosters.Count(p => p.Language == originalLanguage);
                _logger.Info("SUCCESS: Returning {0} posters ({1} in {2}, {3} others)", 
                    sortedPosters.Count, 
                    originalCount,
                    originalLanguage,
                    sortedPosters.Count - originalCount);

                return sortedPosters;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Error processing movie: {movie.Name}", ex);
                return new List<RemoteImageInfo>();
            }
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            // Emby 会自己下载图片
            return Task.FromResult(new HttpResponseInfo());
        }

        // ============ IHasSupportedExternalIdentifiers 实现 ============
        
        /// <summary>
        /// 声明此 Provider 支持通过 TMDB ID 进行搜索
        /// 这是 Emby 4.9.x 的新要求
        /// </summary>
        public string[] GetSupportedExternalIdentifiers()
        {
            return new[] { "Tmdb" };
        }

        // ============ Helper Methods ============

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

            _logger.Debug("Fetching movie info from TMDB...");

            try
            {
                using var response = await _httpClient.GetResponse(options);
                using var stream = response.Content;
                using var reader = new StreamReader(stream);
                var jsonString = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("original_language", out var langElement))
                {
                    return langElement.GetString();
                }

                _logger.Warn("No 'original_language' in TMDB response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Failed to fetch movie info for TMDB ID: {tmdbId}", ex);
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

            _logger.Debug("Fetching posters from TMDB...");

            try
            {
                using var response = await _httpClient.GetResponse(options);
                using var stream = response.Content;
                using var reader = new StreamReader(stream);
                var jsonString = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(jsonString);
                
                if (!doc.RootElement.TryGetProperty("posters", out var postersProperty))
                {
                    _logger.Warn("No 'posters' in TMDB response");
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
                        _logger.Debug("✓ Original language: {0}", filePath);
                    }
                    else
                    {
                        otherPosters.Add(imageInfo);
                    }
                }

                // 原语言海报优先
                var sortedList = new List<RemoteImageInfo>(matchingPosters);
                sortedList.AddRange(otherPosters);

                _logger.Info("Found {0} original language posters, {1} others", 
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