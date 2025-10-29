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
            _logger.Info("=== ServerEntryPoint constructor called ===");
        }

        /// <summary>
        /// Emby 服务器启动并完成初始化后调用。
        /// 用于执行插件的一次性初始化任务。
        /// </summary>
        public void Run()
        {
            _logger.Info("=== OriginalPoster plugin LOADED ===");
            
            if (Plugin.Instance == null)
            {
                _logger.Error("Plugin.Instance is NULL in Run()!");
                return;
            }

            var config = Plugin.Instance.PluginConfiguration;
            if (config == null)
            {
                _logger.Error("Configuration is NULL in Run()!");
                return;
            }

            _logger.Info("Plugin Enabled: {0}", config.EnablePlugin);
            _logger.Info("TMDB API Key Configured: {0}", !string.IsNullOrEmpty(config.TmdbApiKey));
            
            if (string.IsNullOrEmpty(config.TmdbApiKey))
            {
                _logger.Warn("⚠️  TMDB API Key is NOT configured! Plugin will not work until configured.");
            }
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