using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
// 1. 不再需要 HttpClient，可以移除
// using MediaBrowser.Common.Net; 
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
// 2. 不再需要引用 Providers 命名空间来手动创建
// using OriginalPoster.Providers; 
using System;
using System.Linq;

namespace OriginalPoster
{
    // 3. 不再需要 IDisposable，因为我们不手动管理 _imageProvider
    public class ServerEntryPoint : IServerEntryPoint 
    {
        private readonly ILogger _logger;
        private readonly ILogManager _logManager;
        // 4. 移除 _httpClient 和 _imageProvider 字段
        // private readonly IHttpClient _httpClient;
        private readonly IProviderManager _providerManager;
        // private OriginalLanguageImageProvider _imageProvider;

        // 5. 从构造函数中移除 IHttpClient
        public ServerEntryPoint(ILogManager logManager, IProviderManager providerManager)
        {
            _logManager = logManager;
            _logger = logManager.GetLogger(GetType().Name);
            // _httpClient = httpClient; // 移除
            _providerManager = providerManager;
            _logger.Info("=== ServerEntryPoint constructor called ===");
        }

        public void Run()
        {
            _logger.Info("╔═══════════════════════════════════════════════════════════════");
            _logger.Info("║ OriginalPoster Plugin Starting");
            _logger.Info("╚═══════════════════════════════════════════════════════════════");
            
            // ... (你关于 Plugin.Instance 和 Configuration 的检查都很好，保留它们) ...
            
            _logger.Info("✓ Plugin.Instance OK");
            var config = Plugin.Instance.PluginConfiguration;
            // ... (保留配置检查日志) ...

            // 6. 移除所有手动创建 Provider 的代码
            /*
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
            */

            // 7. 保留你所有的诊断日志！
            // 这些日志现在应该会显示成功
            _logger.Info("───────────────────────────────────────────────────────────────");
            _logger.Info("Checking registered Image Providers (after DI registration)...");
            
            try
            {
                var allProviders = _providerManager.ImageProviders;
                // ... (保留所有日志) ...

                // 重点：现在这里应该会成功！
                var ourProvider = allProviders.FirstOrDefault(p => p.Name == "OriginalPoster");
                if (ourProvider != null)
                {
                    _logger.Info("✓✓✓ OriginalPoster IS in ImageProviders list! ✓✓✓");
                }
                else
                {
                    _logger.Error("❌ OriginalPoster NOT in ImageProviders list!");
                }
            }
            catch (Exception ex)
            {
                 _logger.ErrorException("Failed to check providers", ex);
            }
            
            // ... (保留你所有的 Movie 类型测试日志) ...
            // 重点：现在这里也应该会成功！
        }

        // 8. 移除 Dispose 方法
        /*
        public void Dispose()
        {
            _logger.Info("OriginalPoster plugin unloading...");
            _imageProvider = null;
        }
        */
    }
}