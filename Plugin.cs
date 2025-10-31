using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Serialization;
using System;

namespace OriginalPoster
{
    /// <summary>
    /// 插件主类 - 使用 BasePluginSimpleUI 自动生成配置界面
    /// </summary>
    public class Plugin : BasePluginSimpleUI<PluginConfiguration>
    {
        public override Guid Id => new Guid("2DE6B212-1C77-EFBC-8B95-A45F6DAE8921");
        
        public override string Name => "TMDB Original Language";
        
        public override string Description => "Automatically fetches movie posters in their original language from TMDB";
        
        // 插件实例（供其他类访问配置）
        public static Plugin Instance { get; private set; }
        
        public Plugin(IApplicationHost applicationPaths)
            : base(applicationPaths)
        {
            Instance = this;
        }
    }
}