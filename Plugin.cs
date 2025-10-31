using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace OriginalPoster
{
    /// <summary>
    /// 插件主类 - 第一阶段基础版本
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // TODO: 使用 uuidgen 命令生成你自己的GUID并替换下面的值
        // 在Mac终端运行: uuidgen
        public override Guid Id => new Guid("2DE6B212-1C77-EFBC-8B95-A45F6DAE8921");
        
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
            // ======================================================
            // ### 开始调试代码 ###
            // 临时添加此代码来检查 DLL 中实际嵌入的资源名称
            try
            {
                var assembly = GetType().Assembly;
                var resourceNames = assembly.GetManifestResourceNames();
                
                Console.WriteLine("[OriginalPoster] --- 检查嵌入式资源 ---");
                if (resourceNames.Length == 0)
                {
                    Console.WriteLine("[OriginalPoster] !! 警告：未在此插件 DLL 中找到任何嵌入式资源。");
                }
                
                foreach (var name in resourceNames)
                {
                    // 打印出所有找到的资源名
                    Console.WriteLine($"[OriginalPoster] 发现资源: {name}");
                }
                Console.WriteLine("[OriginalPoster] --- 检查结束 ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OriginalPoster] 查找资源时出错: {ex.Message}");
            }
            // ### 结束调试代码 ###
            // ======================================================
        }
        
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
