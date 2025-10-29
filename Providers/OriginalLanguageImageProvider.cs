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
        public int Order => -1; // 更高优先级，在 TMDB 之前

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public OriginalLanguageImageProvider(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _logger.Info("OriginalLanguageImageProvider initialized.");
        }

        public bool Supports(BaseItem item)
        {
            var isMovie = item is Movie;
            var hasTmdbId = item.HasProviderId(MetadataProviders.Tmdb);
            
            _logger.Debug("Supports check: Item={0}, IsMovie={1}, HasTmdbId={2}", 
                item.Name, isMovie, hasTmdbId);
            
            return isMovie && hasTmdbId;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            _logger.Info("=== GetImages called for: {0} ===", item.Name);

            // 检查插件是否启用
            if (Plugin.Instance?.PluginConfiguration?.EnablePlugin != true)
            {
                _logger.Info("Plugin is disabled in configuration.");
                return new List<RemoteImageInfo>();
            }

            var movie = item as Movie;
            if (movie == null)
            {
                _logger.Warn("Item is not a Movie, skipping.");
                return new List<RemoteImageInfo>();
            }

            var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb);
            if (string.IsNullOrEmpty(tmdbId))
            {
                _logger.Warn("Movie '{0}' has no TMDB ID.", movie.Name);
                return new List<RemoteImageInfo>();
            }

            var apiKey = Plugin.Instance?.PluginConfiguration?.TmdbApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Error("TMDB API Key not configured! Please configure in plugin settings.");
                return new List<RemoteImageInfo>();
            }

            _logger.Info("Processing: Movie='{0}', TmdbId={1}", movie.Name, tmdbId);

            try
            {
                // 1. 获取原始语言
                var originalLanguage = await GetOriginalLanguageAsync(tmdbId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(originalLanguage))
                {
                    _logger.Warn("Could not get original language for TMDB ID {0}", tmdbId);
                    return new List<RemoteImageInfo>();
                }

                _logger.Info("Original language: {0}", originalLanguage);

                // 2. 获取并排序海报
                var sortedPosters = await GetSortedPostersAsync(tmdbId, apiKey, originalLanguage, cancellationToken);

                _logger.Info("Returning {0} posters ({1} in original language)", 
                    sortedPosters.Count, 
                    sortedPosters.Count(p => p.Language == originalLanguage));

                return sortedPosters;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Error processing movie '{movie.Name}'", ex);
                return new List<RemoteImageInfo>();
            }
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            // Emby 会自行下载 URL，这里返回空实现即可
            return Task.FromResult(new HttpResponseInfo());
        }

        private async Task<string> GetOriginalLanguageAsync(string tmdbId, string apiKey, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}&language=en-US";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true
            };

            _logger.Debug("Fetching movie details: {0}", url);

            using var response = await _httpClient.GetResponse(options);
            using var stream = response.Content;
            using var reader = new StreamReader(stream);
            var jsonString = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(jsonString);
            if (doc.RootElement.TryGetProperty("original_language", out var langElement))
            {
                return langElement.GetString();
            }

            return null;
        }

        private async Task<List<RemoteImageInfo>> GetSortedPostersAsync(string tmdbId, string apiKey, string originalLanguage, CancellationToken cancellationToken)
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/images?api_key={apiKey}";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true
            };

            _logger.Debug("Fetching posters: {0}", url);

            using var response = await _httpClient.GetResponse(options);
            using var stream = response.Content;
            using var reader = new StreamReader(stream);
            var jsonString = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(jsonString);
            var postersArray = doc.RootElement.GetProperty("posters").EnumerateArray();

            var matchingPosters = new List<RemoteImageInfo>();
            var otherPosters = new List<RemoteImageInfo>();

            foreach (var poster in postersArray)
            {
                var isoCode = poster.GetProperty("iso_639_1").GetString();
                var filePath = poster.GetProperty("file_path").GetString();
                
                if (string.IsNullOrEmpty(filePath))
                    continue;

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
                    _logger.Debug("Added original language poster: {0}", filePath);
                }
                else
                {
                    otherPosters.Add(imageInfo);
                }
            }

            // 原语言海报在前
            var sortedList = new List<RemoteImageInfo>(matchingPosters);
            sortedList.AddRange(otherPosters);

            _logger.Info("Found {0} original language posters, {1} other posters", 
                matchingPosters.Count, otherPosters.Count);

            return sortedList;
        }
    }
}
