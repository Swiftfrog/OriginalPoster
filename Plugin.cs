// Plugin.cs
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.Linq;
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

// --- 已修正日志的调试版本 ---
        public readonly ILogger Logger;
        public Stream GetThumbImage()
        {
            var assembly = GetType().Assembly;

            // 1. 这是我们 *猜测* 的名称
            string resourceName = "OriginalPoster.logo.png";

            // 2. 获取 *所有* 实际存在的资源名称
            var allResourceNames = assembly.GetManifestResourceNames();

            // 3. 记录日志 (使用正确的 Warn 方法)
            Logger.Warn("[Original Poster] Debug: 正在尝试加载 Logo...");
            Logger.Warn("[Original Poster] Debug: 猜测的资源名是: {0}", resourceName);
            Logger.Warn("[Original Poster] Debug: 实际找到的所有资源名: {0}", string.Join(", ", allResourceNames));

            // 4. 尝试加载
            var stream = assembly.GetManifestResourceStream(resourceName);

            // 5. 记录结果
            if (stream == null)
            {
                // 使用正确的 Error 方法
                Logger.Error("[Original Poster] 失败! 无法加载资源: {0}. 请检查上面的 '实际找到的所有资源名' 列表, 复制正确的名字并替换 'resourceName' 变量。", resourceName);

                // 尝试加载列表中的第一个(如果有的话), 至少返回点什么
                if (allResourceNames.Any())
                {
                    Logger.Warn("[Original Poster] 尝试回退到第一个找到的资源: {0}", allResourceNames.First());
                    return assembly.GetManifestResourceStream(allResourceNames.First());
                }
            }
            else
            {
                // 使用正确的 Info 方法
                Logger.Info("[Original Poster] 成功加载 Logo: {0}", resourceName);
            }

            return stream;
        }

        public ImageFormat ThumbImageFormat => ImageFormat.png;

//        // 添加这个 ThumbImage 属性
//        public Stream GetThumbImage()
//        {
//            var assembly = GetType().Assembly;
//            
//            // 你的命名空间是 "OriginalPoster"
//            // 你在 .csproj 里嵌入的文件是 "logo.png"
//            // 所以正确的资源名是 "OriginalPoster.logo.png"
//            string resourceName = "OriginalPoster.logo.png";
//            
//            return assembly.GetManifestResourceStream(resourceName);
//        }
//        public ImageFormat ThumbImageFormat => ImageFormat.Png;

    }
}