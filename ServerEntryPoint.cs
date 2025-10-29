using MediaBrowser.Controller.Plugins; // IServerEntryPoint
// using MediaBrowser.Controller.Providers; // IProviderManager (Remove this if not needed elsewhere)
using MediaBrowser.Model.Logging; // ILogger
using System;
// using OriginalPoster.Providers; // OriginalLanguageImageProvider (Remove this if not needed elsewhere)

namespace OriginalPoster
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILogger _logger;
        // private readonly IProviderManager _providerManager; // Remove this
        // private readonly OriginalLanguageImageProvider _imageProvider; // Remove this

        // 构造函数：注入 Emby 的 ILogger
        // Remove IProviderManager and OriginalLanguageImageProvider from constructor
        public ServerEntryPoint(ILogger logger) // Only inject ILogger
        {
            _logger = logger;
        }

        /// <summary>
        /// Emby 服务器启动并完成初始化后调用。
        /// 用于执行插件的一次性初始化任务。
        /// </summary>
        public void Run()
        {
            _logger.Info("OriginalPoster plugin loaded successfully.");
            // Remove the provider registration code
            // The provider should be discovered automatically by Emby if it's correctly implemented and discoverable.
        }

        /// <summary>
        /// Emby 服务器关闭时调用。
        /// 用于清理插件占用的资源。
        /// </summary>
        public void Dispose()
        {
            _logger.Info("OriginalPoster plugin is being unloaded.");
        }
    }
}