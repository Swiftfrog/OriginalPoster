using System.Reflection;
using Tvdb; // Emby 内部命名空间

namespace OriginalPoster.Reflection
{
    public static class Tvdb
    {
        public static readonly Assembly _tvdbAssembly = typeof(TvdbProvider).Assembly;

        public static readonly bool IsSupported = _tvdbAssembly != null;

        public static readonly MethodInfo _ensureMovieInfoTvdb =
            typeof(TvdbProvider).GetMethod("EnsureMovieInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo _ensureSeriesInfoTvdb =
            typeof(TvdbSeriesProvider).GetMethod("EnsureSeriesInfo", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
