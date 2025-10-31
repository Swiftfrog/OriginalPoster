using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace EmbyOriginalPosterPlugin
{
    /// <summary>
    /// 插件主类 - 第一阶段基础版本
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // TODO: 使用 uuidgen 命令生成你自己的GUID并替换下面的值
        // 在Mac终端运行: uuidgen
        public override Guid Id => new Guid("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX");
        
        public override string Name => "TMDB Original Language";
        
        public override string Description => "Automatically fetches movie posters in their original language from TMDB";
        
        // 插件实例（供其他类访问配置）
        public static Plugin Instance { get; private set; }
        
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            
            // 第一阶段：启动时输出日志确认插件加载
            Console.WriteLine($"[OriginalPoster] Plugin loaded, version {Version}");
        }
        
        // 版本号
        public override string Version => "1.0.0";
        
        // 配置页面（第一阶段先返回空，后续添加）
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "TMDB Original Language",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                }
            };
        }
    }
}
