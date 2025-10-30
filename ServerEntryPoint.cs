using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
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
                if (allProviders != null)
                {
                    _logger.Info("Total registered providers: {0}", allProviders.Length);
                    
                    foreach (var provider in allProviders)
                    {
                        var hasOrder = provider as IHasOrder;
                        var order = hasOrder?.Order ?? 0;
                        _logger.Info("  - {0} (Order: {1})", provider.Name, order);
                    }

                    var ourProvider = allProviders.FirstOrDefault(p => p.Name == "OriginalPoster");
                    if (ourProvider != null)
                    {
                        _logger.Info("✓✓✓ OriginalPoster IS registered! ✓✓✓");
                    }
                    else
                    {
                        _logger.Warn("⚠️⚠️⚠️ OriginalPoster NOT found in registered providers!");
                        _logger.Warn("This means Emby hasn't discovered our Provider class.");
                        _logger.Warn("Possible reasons:");
                        _logger.Warn("  1. Provider class not in correct namespace");
                        _logger.Warn("  2. Missing assembly scanning");
                        _logger.Warn("  3. Emby version incompatibility");
                    }
                }
                else
                {
                    _logger.Warn("ImageProviders property is NULL!");
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to check providers", ex);
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