// ServerEntryPoint.cs
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using OriginalPoster.Providers;

namespace OriginalPoster;

public class ServerEntryPoint : IServerEntryPoint
{
    private readonly IApplicationHost _applicationHost;

    public ServerEntryPoint(IApplicationHost applicationHost)
    {
        _applicationHost = applicationHost;
    }

    public Task RunAsync()
    {
        // 注册你的元数据提供者
        _applicationHost.GetServices<IServiceCollection>()
                        .AddSingleton<IMetadataProvider<Movie>, OriginalLanguageMetadataProvider>();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // 无资源需要释放
    }
}
