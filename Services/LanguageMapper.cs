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

    // 新增：国家代码到中文名称的映射
    private static readonly Dictionary<string, string> CountryToChineseNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "US", "美国" }, { "GB", "英国" }, { "CA", "加拿大" }, { "AU", "澳大利亚" },
        { "CN", "中国" }, { "HK", "香港" }, { "TW", "台湾" }, { "SG", "新加坡" },
        { "JP", "日本" }, { "KR", "韩国" },
        { "FR", "法国" }, { "DE", "德国" }, { "ES", "西班牙" }, { "IT", "意大利" },
        { "RU", "俄罗斯" }, { "PT", "葡萄牙" }, { "BR", "巴西" },
        { "SA", "沙特" }, { "EG", "埃及" }, { "IL", "以色列" },
        { "IN", "印度" }, { "TH", "泰国" }, { "VN", "越南" },
        { "SE", "瑞典" }, { "NO", "挪威" }, { "DK", "丹麦" }, { "FI", "芬兰" },
        { "NL", "荷兰" }, { "BE", "比利时" }, { "PL", "波兰" }, { "TR", "土耳其" },
        { "IR", "伊朗" }, { "ID", "印尼" }, { "PH", "菲律宾" }, { "MY", "马来西亚" },
        { "NZ", "新西兰" }, { "MX", "墨西哥" }, { "AR", "阿根廷" }
    };

    public static string GetLanguageForCountry(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return "en-US";
        return CountryToLanguageMap.TryGetValue(countryCode, out var lang) ? lang : "en-US";
    }
}
