using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OriginalPoster.Providers;

namespace OriginalPoster
{
    /// <summary>
    /// 测试用的 API 服务
    /// 访问: http://your-emby:8096/emby/OriginalPoster/Test?movieId=123
    /// </summary>
    [Route("/OriginalPoster/Test", "GET", Summary = "Test OriginalPoster Provider")]
    public class TestProviderRequest : IReturn<TestProviderResponse>
    {
        [ApiMember(Name = "movieId", Description = "Movie ID", IsRequired = true)]
        public long MovieId { get; set; }
    }

    public class TestProviderResponse
    {
        public string Message { get; set; }
        public int PosterCount { get; set; }
    }

    public class TestProviderService : IService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly ILogger _logger;

        public TestProviderService(
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            ILogManager logManager)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _logger = logManager.GetLogger("TestProviderService");
        }

        public async Task<object> Get(TestProviderRequest request)
        {
            _logger.Info("╔═══════════════════════════════════════════════════════════");
            _logger.Info("║ TEST API CALLED - Movie ID: {0}", request.MovieId);
            _logger.Info("╚═══════════════════════════════════════════════════════════");

            try
            {
                // 获取电影
                var item = _libraryManager.GetItemById(request.MovieId);
                if (item == null)
                {
                    _logger.Warn("Item not found");
                    return new TestProviderResponse { Message = "Item not found" };
                }

                var movie = item as Movie;
                if (movie == null)
                {
                    _logger.Warn("Item is not a Movie");
                    return new TestProviderResponse { Message = "Not a Movie type" };
                }

                _logger.Info("Found movie: {0}", movie.Name);

                // 获取我们的 Provider
                var allProviders = _providerManager.ImageProviders;
                _logger.Info("Total providers: {0}", allProviders.Length);

                var ourProvider = allProviders.FirstOrDefault(p => p.Name == "OriginalPoster") 
                    as OriginalLanguageImageProvider;

                if (ourProvider == null)
                {
                    _logger.Error("OriginalPoster provider NOT FOUND in ImageProviders!");
                    return new TestProviderResponse 
                    { 
                        Message = "Provider not found" 
                    };
                }

                _logger.Info("Found OriginalPoster provider");

                // 检查是否支持
                var supports = ourProvider.Supports(movie);
                _logger.Info("Provider.Supports(movie) = {0}", supports);

                if (!supports)
                {
                    return new TestProviderResponse 
                    { 
                        Message = "Provider.Supports returned false (check logs for reason)" 
                    };
                }

                // 获取配置
                if (Plugin.Instance?.PluginConfiguration == null)
                {
                    _logger.Error("Plugin configuration not available");
                    return new TestProviderResponse 
                    { 
                        Message = "Plugin configuration not available" 
                    };
                }

                var apiKey = Plugin.Instance.PluginConfiguration.TmdbApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.Error("TMDB API Key not configured!");
                    return new TestProviderResponse 
                    { 
                        Message = "TMDB API Key not configured in plugin settings" 
                    };
                }

                _logger.Info("Calling GetImages...");

                // 获取默认的 LibraryOptions
                var libraryOptions = _providerManager.GetDefaultLibraryOptions("movies");

                // 调用 GetImages
                var images = await ourProvider.GetImages(movie, libraryOptions, CancellationToken.None);
                var imageList = images.ToList();

                _logger.Info("SUCCESS! GetImages returned {0} images", imageList.Count);

                return new TestProviderResponse
                {
                    Message = $"SUCCESS! Retrieved {imageList.Count} posters for '{movie.Name}'",
                    PosterCount = imageList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorException("TEST API ERROR", ex);
                return new TestProviderResponse
                {
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}