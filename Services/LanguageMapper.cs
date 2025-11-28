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
    
    private static readonly Dictionary<string, string> CountryToChineseNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // === 英语国家 ===
        { "US", "美国" }, { "GB", "英国" }, { "CA", "加拿大" }, { "AU", "澳大利亚" }, 
        { "NZ", "新西兰" }, { "IE", "爱尔兰" },

        // === 中文/东亚 ===
        { "CN", "中国" }, { "HK", "香港" }, { "TW", "台湾" }, { "SG", "新加坡" },
        { "JP", "日本" }, { "KR", "韩国" }, { "KP", "朝鲜" }, { "MO", "澳门" },

        // === 历史政权 (老电影常见) ===
        { "SU", "苏联" },            // Soviet Union
        { "CS", "捷克斯洛伐克" },     // Czechoslovakia
        { "YU", "南斯拉夫" },         // Yugoslavia
        { "DD", "东德" },            // East Germany
        { "XC", "捷克斯洛伐克" },     // TMDB 偶尔使用的非标准代码

        // === 欧洲主要 ===
        { "FR", "法国" }, { "DE", "德国" }, { "ES", "西班牙" }, { "IT", "意大利" },
        { "RU", "俄罗斯" }, { "PT", "葡萄牙" }, { "NL", "荷兰" }, { "BE", "比利时" },
        { "SE", "瑞典" }, { "NO", "挪威" }, { "DK", "丹麦" }, { "FI", "芬兰" },
        { "IS", "冰岛" }, { "CH", "瑞士" }, { "AT", "奥地利" }, { "GR", "希腊" },
        
        // === 欧洲其他 ===
        { "PL", "波兰" }, { "TR", "土耳其" }, { "UA", "乌克兰" }, { "CZ", "捷克" },
        { "HU", "匈牙利" }, { "RO", "罗马尼亚" }, { "BG", "保加利亚" }, { "RS", "塞尔维亚" },
        { "HR", "克罗地亚" }, { "SK", "斯洛伐克" },

        // === 美洲 ===
        { "BR", "巴西" }, { "MX", "墨西哥" }, { "AR", "阿根廷" }, { "CL", "智利" },
        { "CO", "哥伦比亚" }, { "PE", "秘鲁" }, { "CU", "古巴" }, { "VE", "委内瑞拉" },

        // === 亚洲其他 ===
        { "IN", "印度" }, { "TH", "泰国" }, { "VN", "越南" }, { "ID", "印尼" },
        { "PH", "菲律宾" }, { "MY", "马来西亚" }, { "PK", "巴基斯坦" }, { "IR", "伊朗" },
        { "IL", "以色列" }, { "SA", "沙特" }, { "AE", "阿联酋" }, { "QA", "卡塔尔" },
        { "KZ", "哈萨克斯坦" },

        // === 非洲 ===
        { "EG", "埃及" }, { "ZA", "南非" }, { "MA", "摩洛哥" }, { "NG", "尼日利亚" },
        { "KE", "肯尼亚" }
    };
    

    /// <summary>
    /// 将代码转换为 "美国 (US)" 格式
    /// </summary>
    public static string GetCountryTag(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return string.Empty;

        // 尝试获取中文名
        if (CountryToChineseNameMap.TryGetValue(countryCode, out var name))
        {
            return $"{name} ({countryCode.ToUpper()})";
        }

        // 如果字典里没有，直接返回代码（例如 "XX"）
        return countryCode.ToUpper();
    }    
    
}