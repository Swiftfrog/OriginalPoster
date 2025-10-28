using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common;
using System;
using System.IO;
using OriginalPoster.Configuration; // 注意命名空间

namespace OriginalPoster
{
    /// <summary>
    /// 独立的“优先使用原画报”插件。
    /// </summary>
    public class Plugin : BasePlugin, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            // 配置文件路径
            var configPath = Path.Combine(applicationPaths.PluginConfigurationsPath, "prefer_original_poster.xml");
            Configuration = ConfigurationFactory.LoadConfiguration<PluginConfiguration>(configPath, xmlSerializer);
        }

        public override string Name => "Prefer Original Poster";

        // 请务必生成一个新的、唯一的 GUID！
        public override Guid Id => Guid.Parse("87EB8757-BCCC-A33F-8AF5-2BDE286E6956");

        public PluginConfiguration Configuration { get; private set; }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var configPath = Path.Combine(ApplicationPaths.PluginConfigurationsPath, "prefer_original_poster.xml");
            Configuration = (PluginConfiguration)configuration;
            ConfigurationFactory.SaveConfiguration(configPath, Configuration, XmlSerializer);
            
            // 根据新配置应用或取消补丁
            if (Configuration.EnablePreferOriginalPoster)
            {
                _ = new PreferOriginalPoster(); // 会自动调用 Patch()
            }
            else
            {
                // 如果插件支持 Unpatch，这里可以调用
                // 目前您的代码似乎没有显式的 Unpatch，但 Harmony 通常可以处理
            }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
