using MediaBrowser.Common;
using Microsoft.Extensions.DependencyInjection;
using OriginalPoster.Providers; // 确保命名空间正确

namespace OriginalPoster // 确保命名空间正确
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            // 注册你的 IMetadataProvider
            serviceCollection.AddSingleton<IMetadataProvider<Movie>, OriginalLanguageMetadataProvider>();

            // 如果你决定回退到 IRemoteImageProvider，也可以注册：
            // serviceCollection.AddSingleton<IRemoteImageProvider, OriginalLanguageImageProvider>();

            // 也可以注册其他需要的服务，例如用于缓存的 IMemoryCache
            // serviceCollection.AddMemoryCache();
        }
    }
}
