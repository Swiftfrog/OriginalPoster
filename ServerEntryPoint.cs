using MediaBrowser.Controller.Plugins; // IServerEntryPoint
using MediaBrowser.Model.Logging; // ILogger, ILogManager
using System;

namespace OriginalPoster
{
    public class ServerEntryPoint : IServerEntryPoint, IDisposable
    {
        private readonly ILogger _logger;

        // 修复：注入 ILogManager 而不是 ILogger
        public ServerEntryPoint(ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _logger.Info("ServerEntryPoint constructor called.");
        }

        /// <summary>
        /// Emby 服务器启动并完成初始化后调用。
        /// 用于执行插件的一次性初始化任务。
        /// </summary>
        public void Run()
        {
            _logger.Info("OriginalPoster plugin loaded successfully.");
            _logger.Info("Plugin enabled: {0}", Plugin.Instance?.Configuration?.EnablePlugin ?? false);
            _logger.Info("TMDB API Key configured: {0}", !string.IsNullOrEmpty(Plugin.Instance?.Configuration?.TmdbApiKey ?? ""));
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