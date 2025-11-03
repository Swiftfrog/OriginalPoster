// Plugin.cs
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
    public class Plugin : BasePluginSimpleUI<OriginalPosterConfig>
    {
        public override Guid Id => new Guid("2DE6B212-1C77-EFBC-8B95-A45F6DAE8921");
        
        public override string Name => "Original Poster TMDB";
        
        public override string Description => "Automatically fetches movie posters in their original language from TMDB";
        
        // 插件实例（供其他类访问配置）
        public static Plugin Instance { get; private set; }
        
         public OriginalPosterConfig Configuration => GetOptions();
        
        public Plugin(IApplicationHost applicationPaths)
            : base(applicationPaths)
        {
            Instance = this;
        }

        // --- 2. 添加这个 ThumbImage 属性 ---
        public Stream ThumbImage
        {
            get
            {
                // 获取当前程序集
                var assembly = GetType().Assembly;

                // 构造资源的清单名称
                // 格式: [默认命名空间].[文件夹(如果有)].[文件名]
                // 
                // 你的命名空间是 "OriginalPoster"
                // 假设你的 logo 文件叫 "logo.png" 并且在项目根目录
                string resourceName = "OriginalPoster.logo.png";

                // **** 注意 ****
                // 如果你把 logo.png 放在了一个叫 "Images" 的文件夹里
                // 那么资源名应该是: "OriginalPoster.Images.logo.png"
                
                return assembly.GetManifestResourceStream(resourceName);
            }
        }

    }
}