using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace OriginalPoster.Services;

public class LanguageCacheManager
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly string _cacheFilePath;
    private Dictionary<string, string> _cache;
    private readonly object _writeLock = new object();

    public LanguageCacheManager(IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
        // 将缓存文件保存在插件配置目录，名为 OriginalPoster.Cache.json
        _cacheFilePath = Path.Combine(applicationPaths.PluginConfigurationsPath, "OriginalPoster.Cache.json");
        _cache = LoadCache();
    }

    /// <summary>
    /// 尝试从缓存获取语言
    /// </summary>
    public bool TryGetLanguage(string tmdbId, string type, out string? language)
    {
        string key = GetKey(tmdbId, type);
        lock (_writeLock) // 读取也加锁，防止读取时正在写入导致集合修改异常
        {
            return _cache.TryGetValue(key, out language);
        }
    }

    /// <summary>
    /// 添加并保存到缓存
    /// </summary>
    public void AddAndSave(string tmdbId, string type, string language)
    {
        string key = GetKey(tmdbId, type);
        
        lock (_writeLock)
        {
            // 如果已存在且相同，不重复写入 IO
            if (_cache.TryGetValue(key, out var existing) && existing == language)
                return;

            _cache[key] = language;
            SaveCache();
        }
    }

    private string GetKey(string tmdbId, string type) => $"{type}_{tmdbId}";

    private Dictionary<string, string> LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                if (!string.IsNullOrEmpty(json))
                {
                    return _jsonSerializer.DeserializeFromString<Dictionary<string, string>>(json) 
                           ?? new Dictionary<string, string>();
                }
            }
        }
        catch { /* 忽略读取错误，使用空字典 */ }
        
        return new Dictionary<string, string>();
    }

    private void SaveCache()
    {
        try
        {
            // 序列化并写入文件
            // 注意：对于几千条数据，JSON 写入非常快。
            // 如果数据量达到几十万，可能需要优化保存策略（如每隔几分钟保存一次），目前实时保存即可。
            var json = _jsonSerializer.SerializeToString(_cache);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch { /* 忽略写入错误 */ }
    }
}
