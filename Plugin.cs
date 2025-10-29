using MediaBrowser.Controller; // IServerApplicationHost
using MediaBrowser.Controller.Plugins; // BasePluginSimpleUI
using MediaBrowser.Common.Plugins;
using System;
using OriginalPoster.Config;

namespace OriginalPoster;

public class Plugin : BasePluginSimpleUI<OriginalPosterConfig>
{
    public override string Name => "OriginalPoster";
    public override string Description => "优先显示影视作品原生语言的海报。";
    public override Guid Id => new("09872246-4676-EBD7-E81C-9B95E12A832B");

    // 构造函数：BasePluginSimpleUI 要求传入 IApplicationHost
    public Plugin(IServerApplicationHost applicationHost)
        : base(applicationHost)
    {
        Instance = this;
    }

    // 单例访问点（便于其他类获取配置）
    public static Plugin Instance { get; private set; } = null!;

    // 便捷属性：从插件内部获取当前配置
    public OriginalPosterConfig Configuration => GetOptions();
}
