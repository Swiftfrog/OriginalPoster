using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using OriginalPoster.Providers;
using System;
using System.Linq;

namespace OriginalPoster
{
    public class ServerEntryPoint : IServerEntryPoint, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILogManager _logManager;
        private readonly IHttpClient _httpClient;
        private readonly IProviderManager _providerManager;
        private OriginalLanguageImageProvider _imageProvider;

        public ServerEntryPoint(ILogManager logManager, IHttpClient httpClient, IProviderManager providerManager)
        {
            _logManager = logManager;
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _providerManager = providerManager;
            _logger.Info("=== ServerEntryPoint constructor called ===");
        }

        public void Run()
        {
            _logger.Info("╔═══════════════════════════════════════════════════════════════");
            _logger.Info("║ OriginalPoster Plugin Starting");
            _logger.Info("╚═══════════════════════════════════════════════════════════════");
            
            // 检查 Plugin 实例
            if (Plugin.Instance == null)
            {
                _logger.Error("❌ Plugin.Instance is NULL!");
                return;
            }
            _logger.Info("✓ Plugin.Instance OK");

            // 检查配置
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

            // 创建 Provider 实例
            try
            {
                _imageProvider = new OriginalLanguageImageProvider(_logManager, _httpClient);
                _logger.Info("✓ Provider instance created");
                _logger.Info("  - Provider Name: {0}", _imageProvider.Name);
                _logger.Info("  - Provider Order: {0}", _imageProvider.Order);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("❌ Failed to create Provider", ex);
                return;
            }

            // 诊断：列出所有已注册的 Image Providers
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
                        var providerType = provider.GetType().FullName;
                        var hasOrder = provider as IHasOrder;
                        var order = hasOrder?.Order ?? 0;
                        var isRemote = provider is IRemoteImageProvider;
                        
                        _logger.Info("  - {0} (Type: {1}, Order: {2}, Remote: {3})", 
                            provider.Name, 
                            providerType,
                            order,
                            isRemote);
                    }

                    // 检查我们的 Provider
                    var ourProvider = allProviders.FirstOrDefault(p => p.Name == "OriginalPoster");
                    var testProvider = allProviders.FirstOrDefault(p => p.Name == "TestOriginalPoster");
                    
                    if (ourProvider != null)
                    {
                        _logger.Info("✓✓✓ OriginalPoster IS in ImageProviders list! ✓✓✓");
                    }
                    else
                    {
                        _logger.Error("❌ OriginalPoster NOT in ImageProviders list!");
                    }
                    
                    if (testProvider != null)
                    {
                        _logger.Info("✓✓✓ TestOriginalPoster IS in ImageProviders list! ✓✓✓");
                    }
                    else
                    {
                        _logger.Error("❌ TestOriginalPoster NOT in ImageProviders list!");
                    }
                    
                    // 检查构造函数是否被调用但没注册
                    if (_imageProvider != null && ourProvider == null)
                    {
                        _logger.Error("⚠️⚠️⚠️ CRITICAL: Provider constructed but NOT registered!");
                        _logger.Error("This suggests Emby's DI container created the instance but didn't add it to ImageProviders");
                        _logger.Error("Possible causes:");
                        _logger.Error("  1. Provider initialization failed after construction");
                        _logger.Error("  2. Provider was filtered out due to configuration");
                        _logger.Error("  3. Emby version compatibility issue");
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
            
            // 额外诊断：检查对于 Movie 类型，哪些 Provider 会被使用
            _logger.Info("───────────────────────────────────────────────────────────────");
            _logger.Info("Testing provider availability for Movie type...");
            
            try
            {
                // 创建一个测试用的 Movie 对象（仅用于检查）
                var testMovie = new Movie
                {
                    Name = "Test Movie"
                };
                
                // 使用扩展方法设置 Provider ID
                testMovie.ProviderIds = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Tmdb", "12345" }
                };
                
                var testLibraryOptions = _providerManager.GetDefaultLibraryOptions("movies");
                
                _logger.Info("Getting enabled providers for test movie...");
                var enabledProviders = _providerManager.GetRemoteImageProviderInfo(testMovie, testLibraryOptions);
                
                _logger.Info("Enabled Image Providers for Movies:");
                foreach (var provider in enabledProviders)
                {
                    _logger.Info("  - {0}", provider.Name);
                }
                
                var ourProviderEnabled = enabledProviders.Any(p => p.Name == "OriginalPoster");
                if (ourProviderEnabled)
                {
                    _logger.Info("✓✓✓ OriginalPoster IS enabled for movies! ✓✓✓");
                }
                else
                {
                    _logger.Error("❌❌❌ OriginalPoster is NOT enabled for movies!");
                    _logger.Error("This is why it's not being called during refresh!");
                    _logger.Error("");
                    _logger.Error("TO FIX THIS:");
                    _logger.Error("1. Go to Emby Dashboard → Libraries");
                    _logger.Error("2. Find your movie library → Click ⋮ → Manage Library");
                    _logger.Error("3. Go to the tab with image/fetcher settings");
                    _logger.Error("4. Enable 'OriginalPoster' provider");
                    _logger.Error("5. Save and try refreshing metadata again");
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error testing provider availability", ex);
            }
            
            _logger.Info("═══════════════════════════════════════════════════════════════");
        }

        public void Dispose()
        {
            _logger.Info("OriginalPoster plugin unloading...");
            _imageProvider = null;
        }
    }
}