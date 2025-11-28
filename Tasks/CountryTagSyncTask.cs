using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging; // ✅ 修正这里：使用 Emby 的日志命名空间
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using OriginalPoster.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OriginalPoster.ScheduledTasks;

public class CountryTagSyncTask : IScheduledTask
{
    private readonly IHttpClient _httpClient;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger; // ✅ 修正这里：去掉泛型 <CountryTagSyncTask>

    public string Name => "OriginalPoster: Sync Country Tags from TMDB";
    public string Key => "OriginalPosterCountryTagSync";
    public string Description => "Fetches origin_country from TMDB and adds them as tags. (Respects API limits)";
    public string Category => "OriginalPoster";

    // ✅ 修正构造函数：ILogger 不带泛型
    public CountryTagSyncTask(
        IHttpClient httpClient,
        IJsonSerializer jsonSerializer,
        ILibraryManager libraryManager,
        ILogManager logManager) // 通常注入 ILogManager 更稳妥，或者直接 ILogger
    {
        _httpClient = httpClient;
        _jsonSerializer = jsonSerializer;
        _libraryManager = libraryManager;
        _logger = logManager.GetLogger(GetType().Name); // ✅ 修正这里：从 LogManager 获取 Logger
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrEmpty(config.TmdbApiKey))
        {
            _logger.Error("OriginalPoster: TMDB API Key is missing. Task aborted."); // ✅ LogError -> Error
            return;
        }

        if (!config.AddCountryTags)
        {
            _logger.Info("OriginalPoster: 'Auto Add Country Tags' setting is disabled. Task aborted."); // ✅ LogWarning -> Info
            return;
        }

        var tmdbClient = new TmdbClient(_httpClient, _jsonSerializer, config.TmdbApiKey);

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            Recursive = true
            // HasTmdbId = true // 注释掉以防版本兼容问题，我们在循环里判断
        };
        
        var items = _libraryManager.GetItemList(query);
        int totalCount = items.Length;
        int processedCount = 0;
        int updatedCount = 0;

        _logger.Info($"OriginalPoster: Found {totalCount} items to check."); // ✅ LogInformation -> Info

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            processedCount++;
            progress.Report((double)processedCount / totalCount * 100);

            var tmdbId = item.ProviderIds.GetValueOrDefault(MetadataProviders.Tmdb.ToString());
            if (string.IsNullOrEmpty(tmdbId)) continue;

            string? type = item switch
            {
                Movie => "movie",
                Series => "tv",
                _ => null
            };

            if (type == null) continue;

            try
            {
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
                        _libraryManager.UpdateItem(item, item.Parent, ItemUpdateType.MetadataImport);
                        updatedCount++;
                        
                        if (updatedCount % 50 == 0)
                        {
                            _logger.Info($"OriginalPoster: Updated tags for {updatedCount} items so far...");
                        }
                    }
                }

                await Task.Delay(250, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"OriginalPoster: Error processing {item.Name} (ID: {tmdbId}): {ex.Message}");
            }
        }

        _logger.Info($"OriginalPoster: Tag Sync Task Completed. Updated {updatedCount} items out of {totalCount}.");
    }
}