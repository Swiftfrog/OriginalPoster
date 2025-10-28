using System.Reflection;
using MediaBrowser.Controller.Providers;

namespace OriginalPoster.Reflection
{
    public static class EmbyProviders
    {
        public static readonly MethodInfo _getAvailableRemoteImages =
            typeof(RemoteImageService).GetMethod("GetAvailableRemoteImages", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo _populateSeasonImagesFromSeasonOrSeriesFolder =
            typeof(LocalImageProvider).GetMethod("PopulateSeasonImagesFromSeasonOrSeriesFolder", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
