using MediaBrowser.Controller.Plugins; // IServerEntryPoint
using MediaBrowser.Controller.Providers; // IProviderManager
using MediaBrowser.Model.Logging; // ILogger
using System;
using OriginalPoster.Providers; // 引入我们的提供者类

namespace OriginalPoster
{
    public class ServerEntryPoint : IServerEntryPoint, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager; // 注入 IProviderManager
        private readonly OriginalLanguageImageProvider _imageProvider; // 保存提供者实例

        // 构造函数：注入 Emby 的 ILogger 和 IProviderManager
        public ServerEntryPoint(
            ILogger logger,
            IProviderManager providerManager, // 注入 IProviderManager
            OriginalLanguageImageProvider imageProvider // 注入我们的提供者实例
        )
        {
            _logger = logger;
            _providerManager = providerManager;
            _imageProvider = imageProvider; // 保存实例引用
        }

        /// <summary>
        /// Emby 服务器启动并完成初始化后调用。
        /// 用于执行插件的一次性初始化任务，例如注册提供者。
        /// </summary>
        public void Run()
        {
            _logger.Info("OriginalPoster plugin loaded successfully.");

            // --- 关键步骤：手动注册我们的图像提供者 ---
            if (_providerManager != null && _imageProvider != null)
            {
                try
                {
                    // 将我们的提供者添加到 Emby 的提供者列表中
                    // AddImageProvider 需要一个 IImageProvider，而我们的 OriginalLanguageImageProvider 实现了它
                    _providerManager.AddImageProvider(_imageProvider);
                    _logger.Info($"OriginalPoster Provider '{_imageProvider.Name}' registered successfully with Order {_imageProvider.Order}.");
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Failed to register OriginalPoster Provider.", ex);
                }
            }
            else
            {
                _logger.Error("IProviderManager or OriginalLanguageImageProvider instance is null. Cannot register provider.");
            }
            // --- 关键步骤结束 ---
        }

        /// <summary>
        /// Emby 服务器关闭时调用。
        /// 用于清理插件占用的资源，例如移除已注册的提供者。
        /// </summary>
        public void Dispose()
        {
            // 可选：在插件卸载时，尝试从 ProviderManager 中移除提供者
            // 注意：不是所有版本的 Emby API 都提供 RemoveImageProvider，需要确认
            // 如果没有 Remove 方法，或者移除不成功，通常问题不大，因为插件卸载后实例就不存在了
            // if (_providerManager != null && _imageProvider != null)
            // {
            //     // _providerManager.RemoveImageProvider(_imageProvider); // Check if this method exists
            // }
            _logger.Info("OriginalPoster plugin is being unloaded.");
        }
    }
}
