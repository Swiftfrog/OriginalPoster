using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
// 移除了 using MediaBrowser.Common.Net; 
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
// 移除了 using OriginalPoster.Providers;
using System;
using System.Linq;

namespace OriginalPoster
{
    /// <summary>
    /// 服务器入口点，负责插件启动时的日志记录和诊断
    /// </summary>
    // 移除了 IDisposable 接口
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly ILogManager _logManager;
        private readonly IProviderManager _providerManager;
        // 移除了 _httpClient 和 _imageProvider 字段

        // 构造函数：移除了 IHttpClient
        public ServerEntryPoint(ILogManager logManager, IProviderManager providerManager)
        {
            _logManager = logManager;
            _logger = logManager.GetLogger(GetType().Name);
            _providerManager = providerManager;
            _logger.Info("=== ServerEntryPoint constructor called ===");
        }

        public void Run()
        {
            _logger.Info("╔═══════════════════════════════════════════════════════════════");
            _logger.Info("║ OriginalPoster Plugin Starting");
            _logger.Info("╚═══════════════════════════════════════════════════════════════");
            
            // 检查 Plugin 实例 (保留)
            if (Plugin.Instance == null)
            {
                _logger.Error("❌ Plugin.Instance is NULL!");
                return;
            }
            _logger.Info("✓ Plugin.Instance OK");

            // 检查配置 (保留)
            var config = Plugin.Instance.PluginConfiguration;
            if (config == null)
            {
                _logger.Error("❌ Configuration is NULL!");
                return;
            }
            _logger.Info("✓ Configuration OK");
            _logger.Info("  - Plugin Enabled: {0}", config.EnablePlugin);
            _logger.Info("  - API Key Configured: {0}", !string.IsNullOrEmpty(config.TmdbApiKey));
            
            if (string.IsNullOrEmpty(config.TmdbApiKey))
            {
                _logger.Warn("⚠️  TMDB API Key NOT configured!");
            }

            // 
            // 移除了所有手动创建 Provider 的代码 (new OriginalLanguageImageProvider)
            // 

            // 诊断：列出所有已注册的 Image Providers (保留并改进)
            try
            {
                _logger.Info("───────────────────────────────────────────────────────────────");
                _logger.Info("Checking registered Image Providers...");
                
                var allProviders = _providerManager.ImageProviders;
                if (allProviders != null && allProviders.Length > 0)
                {
                    _logger.Info("Total registered providers: {0}", allProviders.Length);
                    
                    foreach (var provider in allProviders)
                    {
                        _logger.Info("  - {0} (Type: {1})", provider.Name, provider.GetType().FullName);
                    }

                    // 检查我们的 Provider
                    var ourProvider = allProviders.FirstOrDefault(p => p.Name == "OriginalPoster");
                    
                    if (ourProvider != null)
                    {
                        _logger.Info("✓✓✓ OriginalPoster IS in ImageProviders list! (DI Success) ✓✓✓");
                    }
                    else
                    {
                        _logger.Error("❌ OriginalPoster NOT in ImageProviders list!");
                        _logger.Error("  (This indicates DI scanning failed or provider was filtered)");
                    }
                }
                else
                {
                    _logger.Warn("ImageProviders property is NULL or empty!");
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to check providers", ex);
            }

            _logger.Info("═══════════════════════════════════════════════════════════════");
            
            // 额外诊断：检查对于 Movie 类型，哪些 Provider 会被使用 (保留)
            _logger.Info("───────────────────────────────────────────────────────────────");
            _logger.Info("Testing provider availability for Movie type...");
            
            try
            {
                var testMovie = new Movie
                {
                    Name = "Test Movie",
                    ProviderIds = new ProviderIdDictionary()
                };
                testMovie.ProviderIds.Add("Tmdb", "12345");
                
                var testLibraryOptions = _providerManager.GetDefaultLibraryOptions("movies");
                
                _logger.Info("Getting enabled providers for test movie (which has Tmdb)...");
                var enabledProviders = _providerManager.GetRemoteImageProviderInfo(testMovie, testLibraryOptions);
                
                _logger.Info("Enabled Image Providers for this Movie:");
                foreach (var provider in enabledProviders)
                {
                    _logger.Info("  - {0}", provider.Name);
                }
                
                var ourProviderEnabled = enabledProviders.Any(p => p.Name == "OriginalPoster");
                if (ourProviderEnabled)
                {
                    _logger.Info("✓✓✓ OriginalPoster IS enabled for movies! (Filter Success) ✓✓✓");
                }
                else
                {
                    _logger.Error("❌❌❌ OriginalPoster is NOT enabled for movies!");
                    _logger.Error("This is why it's not being called during refresh!");
                    _logger.Error("Checklist:");
                    _logger.Error("  1. Did you fix GetSupportedExternalIdentifiers() in Provider?");
                    _logger.Error("  2. Did you enable 'OriginalPoster' in Library settings?");
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error testing provider availability", ex);
            }
            
            _logger.Info("═══════════════════════════════════════════════════════════════");
        }

        // 移除了 Dispose() 方法
    }
}