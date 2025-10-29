using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using System;
using OriginalPoster.Config;

namespace OriginalPoster
{
    public class Plugin : BasePluginSimpleUI<OriginalPosterConfig>
    {
        public override string Name => "OriginalPoster";
        public override string Description => "优先显示影视作品原生语言的海报。";
        public override Guid Id => new Guid("09872246-4676-EBD7-E81C-9B95E12A832B");

        private readonly ILogger _logger;

        // 构造函数：BasePluginSimpleUI 要求传入 IApplicationHost
        public Plugin(IServerApplicationHost applicationHost, ILogManager logManager)
            : base(applicationHost)
        {
            Instance = this;
            _logger = logManager.GetLogger(GetType().Name);
            _logger.Info("Plugin constructor called. Instance created.");
        }

        // 单例访问点（便于其他类获取配置）
        public static Plugin Instance { get; private set; }

        // 便捷属性：从插件内部获取当前配置
        public OriginalPosterConfig PluginConfiguration => GetOptions();
    }
}