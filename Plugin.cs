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
        
        public override string Name => "OriginalPosterTMDB";
        
        public override string Description => "Automatically fetches posters in their original language from TMDB";
        
        // 插件实例（供其他类访问配置）
        public static Plugin? Instance { get; private set; }
        
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
            
            // 所以正确的资源名是 "OriginalPoster.logo.png"
            string resourceName = "OriginalPoster.OriginalPosterLogo.webp";
            
            var stream = assembly.GetManifestResourceStream(resourceName);
            // 可以选择在此处添加日志，如果资源未找到
            if (stream == null)
            {
                // 日志记录（如果 ILogger 可用）
                // _logger?.LogError($"Could not find embedded resource: {resourceName}");
                // 根据插件设计，可能需要返回一个默认流或抛出异常
                // 当前选择返回 null，与 Stream? 类型一致
            }
            return stream;
        }
        public ImageFormat ThumbImageFormat => ImageFormat.Webp;

    }
}