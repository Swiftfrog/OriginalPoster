// Services/TmdbClient.cs
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;

using OriginalPoster.Models;

namespace OriginalPoster.Services;

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

    public TmdbClient(IHttpClient httpClient, IJsonSerializer jsonSerializer, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    /// <summary>
    /// 获取项目详情（用于获取 production_countries）
    /// </summary>
    public async Task<TmdbItemDetails> GetItemDetailsAsync(
        string tmdbId,
        string type, // "movie", "tv", "collection"
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tmdbId))
            throw new ArgumentException("TMDB ID cannot be null or empty.", nameof(tmdbId));
        
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be null or empty.", nameof(type));

        // var type = isMovie ? "movie" : "tv";
        var url = $"{BaseUrl}/{type}/{tmdbId}?api_key={_apiKey}";

        var options = new HttpRequestOptions
        {
            Url = url,
            CancellationToken = cancellationToken,
            TimeoutMs = 10000
        };

        using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
        using (var stream = response.Content)
        {
            return await _jsonSerializer.DeserializeFromStreamAsync<TmdbItemDetails>(stream).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 获取指定项目（电影/剧集）的图像列表
    /// </summary>
    public async Task<TmdbImageResult> GetImagesAsync(
        string tmdbId,
        string type, // "movie", "tv", "collection", "tv/{seriesId}/season/{seasonNumber}"
        string language,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tmdbId))
            throw new ArgumentException("TMDB ID cannot be null or empty.", nameof(tmdbId));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be null or empty.", nameof(type));

        string url;

        // 处理播出季格式 "SeriesId_S<SeasonNumber>" -> "tv/{seriesId}/season/{seasonNumber}"
        if (type == "tv_season")
        {
            var parts = tmdbId.Split(new[] { "_S" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
            {
                var seriesId = parts[0];
                var seasonNumber = parts[1];
                type = $"tv/{seriesId}/season/{seasonNumber}";
                url = $"{BaseUrl}/{type}/images?" +
                      $"api_key={_apiKey}&" +
                      $"language={language},null";
            }
            else
            {
                throw new ArgumentException($"Invalid composite season TMDB ID format: {tmdbId}", nameof(tmdbId));
            }
        }
        else
        {
            url = $"{BaseUrl}/{type}/images?" +
                  $"api_key={_apiKey}&" +
                  $"language={language},null";
        }

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
