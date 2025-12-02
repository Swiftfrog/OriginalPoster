// Plugin.cs
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;
using System.IO;
using System;

using OriginalPoster.Services;

namespace OriginalPoster;

public class Plugin : BasePluginSimpleUI<OriginalPosterConfig>, IHasThumbImage
{
    public override Guid Id => new Guid("2DE6B212-1C77-EFBC-8B95-A45F6DAE8921");
    
    public override string Name => "OriginalPoster";
    
    public override string Description => "Automatically fetches posters in their original language from TMDB";
    
    public static Plugin? Instance { get; private set; }
    
    public OriginalPosterConfig Configuration => GetOptions();
    
    public LanguageCacheManager CacheManager { get; private set; }
    
    // public Plugin(IApplicationHost applicationPaths)
    //     : base(applicationPaths)
    // {
    //     Instance = this;
    // }

    public Plugin(IApplicationHost applicationHost, IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer)
        : base(applicationHost)
    {
        Instance = this;
        // ✅ 修复点：直接使用注入进来的 applicationPaths
        CacheManager = new LanguageCacheManager(applicationPaths, jsonSerializer);
    }


    public Stream GetThumbImage()
    {
        var assembly = GetType().Assembly;
        string resourceName = "OriginalPoster.OriginalPosterLogo.webp";
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Failed to load embedded logo resource: '{resourceName}'. " +
                "Check that the file is included as <EmbeddedResource> in OriginalPoster.csproj.");
        }
        return stream;
    }
    public ImageFormat ThumbImageFormat => ImageFormat.Webp;
    
}
