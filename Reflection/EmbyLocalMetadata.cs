using System.Reflection;
using MediaBrowser.Providers.Manager;

namespace OriginalPoster.Reflection
{
    public static class EmbyLocalMetadata
    {
        public static readonly MethodInfo _addLocalImage =
            typeof(LocalImageProvider).GetMethod("AddLocalImage", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo _getLocalFiles =
            typeof(LocalImageProvider).GetMethod("GetLocalFiles", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
