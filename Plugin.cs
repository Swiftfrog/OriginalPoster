// Plugin.cs
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;
using System.IO;
using System;

namespace OriginalPoster
{
    /// <summary>
    /// 插件主类 - 使用 BasePluginSimpleUI 自动生成配置界面
    /// </summary>
    public class Plugin : BasePluginSimpleUI<OriginalPosterConfig>, IHasThumbImage
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

        // 添加这个 ThumbImage 属性
        public Stream GetThumbImage()
        {
            var assembly = GetType().Assembly;
            
            // 你的命名空间是 "OriginalPoster"
            // 你在 .csproj 里嵌入的文件是 "logo.png"
            // 所以正确的资源名是 "OriginalPoster.logo.png"
            string resourceName = "OriginalPoster.OriginalPosterLogo.png";
            
            return assembly.GetManifestResourceStream(resourceName);
        }
        public ImageFormat ThumbImageFormat => ImageFormat.Png;

    }
}