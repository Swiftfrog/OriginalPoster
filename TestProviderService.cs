using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster
{
    /// <summary>
    /// 测试用的 API 服务，用于直接触发 Provider
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
        public string OriginalLanguage { get; set; }
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
            _logger = logManager.GetLogger(GetType().Name);
        }

        public async Task<object> Get(TestProviderRequest request)
        {
            _logger.Info("═══════════════════════════════════════════════════════════");
            _logger.Info("Test API called for movie ID: {0}", request.MovieId);

            try
            {
                // 获取电影
                var movie = _libraryManager.GetItemById(request.MovieId) as Movie;
                if (movie == null)
                {
                    _logger.Warn("Movie not found or not a Movie type");
                    return new TestProviderResponse
                    {
                        Message = "Movie not found or invalid type"
                    };
                }

                _logger.Info("Found movie: {0}", movie.Name);
                _logger.Info("Has TMDB ID: {0}", movie.HasProviderId("Tmdb"));

                // 获取我们的 Provider
                var allProviders = _providerManager.ImageProviders;
                var ourProvider = allProviders.FirstOrDefault(p => p.Name == "OriginalPoster") 
                    as Providers.OriginalLanguageImageProvider;

                if (ourProvider == null)
                {
                    _logger.Error("OriginalPoster provider not found!");
                    return new TestProviderResponse
                    {
                        Message = "OriginalPoster provider not found in ImageProviders list"
                    };
                }

                _logger.Info("Found OriginalPoster provider");

                // 检查是否支持
                if (!ourProvider.Supports(movie))
                {
                    _logger.Warn("Provider.Supports returned false");
                    return new TestProviderResponse
                    {
                        Message = "Provider does not support this movie (no TMDB ID?)"
                    };
                }

                _logger.Info("Provider.Supports = true, calling GetImages...");

                // 获取默认的 LibraryOptions
                var libraryOptions = _providerManager.GetDefaultLibraryOptions("movies");

                // 调用 GetImages
                var images = await ourProvider.GetImages(movie, libraryOptions, CancellationToken.None);
                var imageList = images.ToList();

                _logger.Info("GetImages returned {0} images", imageList.Count);

                var originalLangImage = imageList.FirstOrDefault();
                var originalLang = originalLangImage?.Language ?? "unknown";

                return new TestProviderResponse
                {
                    Message = $"Success! Provider returned {imageList.Count} posters for '{movie.Name}'",
                    PosterCount = imageList.Count,
                    OriginalLanguage = originalLang
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in test API", ex);
                return new TestProviderResponse
                {
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}
