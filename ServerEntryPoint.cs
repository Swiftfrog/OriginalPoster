using MediaBrowser.Controller.Plugins; // IServerEntryPoint
using MediaBrowser.Model.Logging; // ILogger
using System;

namespace OriginalPoster
{
    public class ServerEntryPoint : IServerEntryPoint, IDisposable
    {
        private readonly ILogger _logger;

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