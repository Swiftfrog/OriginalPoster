using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;

namespace PreferOriginalPoster.Plugin
{
    public class Plugin : BasePlugin, IServerEntryPoint
    {
        public static Plugin Instance { get; private set; }
        public ILogger Logger { get; }

        public override string Name => "Prefer Original Poster";
        // 生成新 GUID: https://www.guidgenerator.com/
        public override Guid Id => Guid.Parse("d8f3b3a1-5c9e-4f8a-b1c2-3d4e5f6a7b8c");

        public Plugin(ILogManager logManager)
        {
            Instance = this;
            Logger = logManager.GetLogger(Name);
        }

        public void Run()
        {
            try
            {
                PreferOriginalPosterMod.Initialize(Logger);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize PreferOriginalPoster plugin", ex);
            }
        }

        public void Dispose() { }
    }
}
