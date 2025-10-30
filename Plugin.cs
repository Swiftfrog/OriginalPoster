using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using System;
using OriginalPoster.Config;

// 1. 添加这些 using 语句
using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller.Providers;
using OriginalPoster.Providers; // 确保这是你的 OriginalLanguageImageProvider 所在的命名空间

namespace OriginalPoster
{
    // 2. 在类定义中添加 , IHasServices
    public class Plugin : BasePluginSimpleUI<OriginalPosterConfig>, IHasServices
    {
        public override string Name => "OriginalPoster";
        public override string Description => "优先显示影视作品原生语言的海报。";
        public override Guid Id => new Guid("09872246-4676-EBD7-E81C-9B95E12A832B");

        private readonly ILogger _logger;

        public Plugin(IServerApplicationHost applicationHost, ILogManager logManager)
            : base(applicationHost)
        {
            Instance = this;
            _logger = logManager.GetLogger(GetType().Name);
            _logger.Info("Plugin constructor called. Instance created.");
        }

        public static Plugin Instance { get; private set; }

        public OriginalPosterConfig PluginConfiguration => GetOptions();

        // 3. 实现 IHasServices 接口，添加这个方法
        public System.Collections.Generic.IEnumerable<ServiceDescriptor> GetServices()
        {
            // 注册你的 Provider 为 Singleton (单例)
            // 假设你的类名是 OriginalLanguageImageProvider
            yield return new ServiceDescriptor(typeof(OriginalLanguageImageProvider), typeof(OriginalLanguageImageProvider), ServiceLifetime.Singleton);
            
            // 告诉 Emby，这个类是 IImageProvider 的一个实现
            yield return new ServiceDescriptor(typeof(IImageProvider), p => p.GetRequiredService<OriginalLanguageImageProvider>(), ServiceLifetime.Singleton);
            
            // 告诉 Emby，这个类也是 IRemoteImageProvider 的一个实现
            yield return new ServiceDescriptor(typeof(IRemoteImageProvider), p => p.GetRequiredService<OriginalLanguageImageProvider>(), ServiceLifetime.Singleton);
        }
    }
}