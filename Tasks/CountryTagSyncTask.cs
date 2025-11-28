using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using OriginalPoster.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster.Tasks;

public class CountryTagSyncTask : IScheduledTask
{
    private readonly IHttpClient _httpClient;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CountryTagSyncTask> _logger;

    public string Name => "OriginalPoster: Sync Country Tags from TMDB";
    public string Key => "OriginalPosterCountryTagSync";
    public string Description => "Fetches origin_country from TMDB and adds them as tags for Movies and Series. (Respects API limits)";
    public string Category => "OriginalPoster";

    public CountryTagSyncTask(
        IHttpClient httpClient,
        IJsonSerializer jsonSerializer,
        ILibraryManager libraryManager,
        ILogger<CountryTagSyncTask> logger)
    {
        _httpClient = httpClient;
        _jsonSerializer = jsonSerializer;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // 默认不自动运行，建议用户手动触发或自行设置周期
        return Array.Empty<TaskTriggerInfo>();
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrEmpty(config.TmdbApiKey))
        {
            _logger.LogError("OriginalPoster: TMDB API Key is missing. Task aborted.");
            return;
        }

        if (!config.AddCountryTags)
        {
            _logger.LogWarning("OriginalPoster: 'Auto Add Country Tags' setting is disabled. Task aborted.");
            return;
        }

        // 1. 初始化 TMDB 客户端
        var tmdbClient = new TmdbClient(_httpClient, _jsonSerializer, config.TmdbApiKey);

        // 2. 查询所有 电影 和 剧集
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            Recursive = true,
            HasTmdbId = true // 必须有 TMDB ID
        };
        
        // 获取所有项目 ID (只获取 ID 以节省内存)
        var items = _libraryManager.GetItemList(query);
        int totalCount = items.Count;
        int processedCount = 0;
        int updatedCount = 0;

        _logger.LogInformation($"OriginalPoster: Found {totalCount} items to check.");

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            processedCount++;
            progress.Report((double)processedCount / totalCount * 100);

            // 获取 TMDB ID
            var tmdbId = item.ProviderIds.GetValueOrDefault(MetadataProviders.Tmdb.ToString());
            if (string.IsNullOrEmpty(tmdbId)) continue;

            // 确定类型
            string type = item switch
            {
                Movie => "movie",
                Series => "tv",
                _ => null
            };

            if (type == null) continue;

            try
            {
                // ==============================================================
                // 优化策略：
                // 如果这个项目已经包含了一些看起来像国家代码的 Tag（例如 2个字母大写），
                // 我们可以选择跳过它来节省 API。
                // 但为了准确性，这里我们每次都检查，但依靠 Delay 来保护 API。
                // ==============================================================

                // 调用 API 获取详情
                var details = await tmdbClient.GetItemDetailsAsync(tmdbId, type, cancellationToken);

                if (details != null && details.origin_country?.Length > 0)
                {
                    bool tagsChanged = false;

                    foreach (var country in details.origin_country)
                    {
                        if (!item.Tags.Contains(country, StringComparer.OrdinalIgnoreCase))
                        {
                            item.AddTag(country);
                            tagsChanged = true;
                        }
                    }

                    if (tagsChanged)
                    {
                        // 更新数据库
                        _libraryManager.UpdateItem(item, item.Parent, ItemUpdateType.MetadataImport);
                        updatedCount++;
                        // 记录日志 (每更新 50 个记录一次，避免刷屏)
                        if (updatedCount % 50 == 0)
                        {
                            _logger.LogInformation($"OriginalPoster: Updated tags for {updatedCount} items so far...");
                        }
                    }
                }

                // ==============================================================
                // ⚠️ 速率限制保护 (Rate Limiting) ⚠️
                // TMDB 通常限制 40-50 请求/10秒。
                // 设置 250ms 延迟，每秒约 4 个请求，非常安全。
                // ==============================================================
                await Task.Delay(250, cancellationToken);
            }
            catch (Exception ex)
            {
                // 捕获异常，不要让单个失败中断整个任务
                _logger.LogError($"OriginalPoster: Error processing {item.Name} (ID: {tmdbId}): {ex.Message}");
            }
        }

        _logger.LogInformation($"OriginalPoster: Tag Sync Task Completed. Updated {updatedCount} items out of {totalCount}.");
    }
}
