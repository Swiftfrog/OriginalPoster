// Services/TmdbClient.cs
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using OriginalPoster.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster.Services
{
    /// <summary>
    /// TMDB API 客户端，封装网络请求和反序列化
    /// 严格使用 Emby 提供的 IHttpClient 和 IJsonSerializer
    /// </summary>
    public class TmdbClient
    {
        private const string BaseUrl = "https://api.themoviedb.org/3";
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _apiKey;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpClient">Emby 注入的 HTTP 客户端</param>
        /// <param name="jsonSerializer">Emby 注入的 JSON 序列化器</param>
        /// <param name="apiKey">TMDB API 密钥</param>
        public TmdbClient(IHttpClient httpClient, IJsonSerializer jsonSerializer, string apiKey)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        /// <summary>
        /// 获取指定项目（电影/剧集）的图像列表
        /// </summary>
        /// <param name="tmdbId">TMDB ID</param>
        /// <param name="isMovie">是否为电影</param>
        /// <param name="language">目标语言代码（如 "en"）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图像结果</returns>
        public async Task<TmdbImageResult> GetImagesAsync(
            string tmdbId,
            bool isMovie,
            string language,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tmdbId))
                throw new ArgumentException("TMDB ID cannot be null or empty.", nameof(tmdbId));

            var type = isMovie ? "movie" : "tv";

            // 关键：使用 include_image_language={lang},null 同时获取目标语言和无文字海报
            // 参考 TMDB 官方文档 [[20]], [[26]]
            var url = $"{BaseUrl}/{type}/{tmdbId}/images?" +
                      $"api_key={_apiKey}&" +
                      $"include_image_language={language},null";

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                TimeoutMs = 10000
            };

            using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
            using (var stream = response.Content)
            {
                return await _jsonSerializer.DeserializeFromStreamAsync<TmdbImageResult>(stream).ConfigureAwait(false);
            }
        }
    }
}
