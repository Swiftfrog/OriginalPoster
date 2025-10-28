using System.Reflection;
using MovieDb; // Emby 内部命名空间

namespace OriginalPoster.Reflection
{
    public static class MovieDb
    {
        public static readonly Assembly _movieDbAssembly = typeof(MovieDbProvider).Assembly;

        public static readonly bool IsSupported = _movieDbAssembly != null;

        public static readonly MethodInfo _getMovieInfo =
            typeof(MovieDbProvider).GetMethod("GetMovieInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo _ensureSeriesInfo =
            typeof(MovieDbSeriesProvider).GetMethod("EnsureSeriesInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo _getBackdrops =
            typeof(MovieDbProvider).GetMethod("GetBackdrops", BindingFlags.NonPublic | BindingFlags.Static);
    }
}
