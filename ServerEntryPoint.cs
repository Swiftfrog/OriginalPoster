using MediaBrowser.Controller.Plugins; // IServerEntryPoint
using MediaBrowser.Model.Logging; // ILogger
using System;

namespace OriginalPoster
{
    public class ServerEntryPoint : IServerEntryPoint, IDisposable
    {
        private readonly ILogger _logger;

        // 构造函数：注入 Emby 的 ILogger
        public ServerEntryPoint(ILogger logger)
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
            // 在这里可以进行其他初始化工作，例如启动后台任务（如果需要）
        }

        /// <summary>
        /// Emby 服务器关闭时调用。
        /// 用于清理插件占用的资源。
        /// </summary>
        public void Dispose()
        {
            // 在这里进行清理工作，例如停止后台任务、释放非托管资源等
            _logger.Info("OriginalPoster plugin is being unloaded.");
        }
    }
}
