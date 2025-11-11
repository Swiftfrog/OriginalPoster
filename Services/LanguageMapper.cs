// Services/LanguageMapper.cs
using System;
using System.Collections.Generic;

namespace OriginalPoster.Services;

public static class LanguageMapper
{
    private static readonly Dictionary<string, string> CountryToLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // 英语国家
        { "US", "en-US" }, { "GB", "en-GB" }, { "CA", "en-CA" }, { "AU", "en-AU" },
        // 中文地区（关键！）
        { "CN", "zh-CN" }, { "HK", "zh-HK" }, { "TW", "zh-TW" }, { "SG", "zh-SG" },
        // 东亚
        { "JP", "ja-JP" }, { "KR", "ko-KR" },
        // 欧洲主要国家
        { "FR", "fr-FR" }, { "DE", "de-DE" }, { "ES", "es-ES" }, { "IT", "it-IT" },
        { "RU", "ru-RU" }, { "PT", "pt-PT" }, { "BR", "pt-BR" },
        // 中东
        { "SA", "ar-SA" }, { "EG", "ar-EG" }, { "IL", "he-IL" },
        // 南亚
        { "IN", "hi-IN" }, { "TH", "th-TH" }, { "VN", "vi-VN" }
    };

    public static string GetLanguageForCountry(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return "en-US";
        return CountryToLanguageMap.TryGetValue(countryCode, out var lang) ? lang : "en-US";
    }
}
