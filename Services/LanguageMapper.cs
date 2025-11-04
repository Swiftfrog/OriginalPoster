// Services/LanguageMapper.cs
using System;
using System.Collections.Generic;

namespace OriginalPoster.Services
{
    /// <summary>
    /// 国家代码 (ISO 3166-1) 到 BCP 47 语言标签的映射器
    /// 用于根据电影制片国家自动选择原语言（含地区变体）
    /// </summary>
    public static class LanguageMapper
    {
        /// <summary>
        /// 静态映射表：国家代码 → BCP 47 语言标签
        /// 数据源：TMDB 官方支持的 primary_translations 列表
        /// </summary>
        private static readonly Dictionary<string, string> CountryToLanguageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // 阿拉伯语地区（每个国家一个映射）
            { "AE", "ar-AE" }, { "BH", "ar-BH" }, { "EG", "ar-EG" }, { "IQ", "ar-IQ" },
            { "JO", "ar-JO" }, { "LY", "ar-LY" }, { "MA", "ar-MA" }, { "QA", "ar-QA" },
            { "SA", "ar-SA" }, { "TD", "ar-TD" }, { "YE", "ar-YE" },
        
            // 欧洲（主语言优先）
            { "BY", "be-BY" }, { "BG", "bg-BG" }, { "CZ", "cs-CZ" }, { "DK", "da-DK" },
            { "DE", "de-DE" }, { "AT", "de-AT" }, { "CH", "de-CH" },
            { "GR", "el-GR" }, // { "CY", "el-CY" }, ← 如果 CY 不重要，可省略
            { "GB", "en-GB" }, { "IE", "en-IE" },
            { "US", "en-US" }, { "CA", "en-CA" }, { "AU", "en-AU" }, // 其他英语国家...
            { "ES", "es-ES" }, { "MX", "es-MX" }, { "AR", "es-AR" }, // 西班牙语国家
            { "FR", "fr-FR" }, { "CA", "fr-CA" }, { "BE", "fr-BE" }, // 法语国家
            { "IT", "it-IT" }, // { "VA", "it-VA" },
            { "NL", "nl-NL" }, // { "BE", "nl-BE" }, ← 注意：BE 已映射为 fr-BE，不能重复
            { "PL", "pl-PL" },
            { "PT", "pt-PT" }, { "BR", "pt-BR" },
            { "RO", "ro-RO" }, // { "MD", "ro-MD" },
            { "RU", "ru-RU" },
            { "SE", "sv-SE" }, { "NO", "no-NO" }, { "FI", "fi-FI" },
            { "HU", "hu-HU" }, { "HR", "hr-HR" }, { "SK", "sk-SK" }, { "SI", "sl-SI" },
            { "LV", "lv-LV" }, { "LT", "lt-LT" }, { "EE", "et-EE" },
        
            // 亚洲
            { "CN", "zh-CN" }, { "HK", "zh-HK" }, { "TW", "zh-TW" }, { "SG", "zh-SG" },
            { "JP", "ja-JP" },
            { "KR", "ko-KR" },
            { "IN", "hi-IN" }, // { "BD", "bn-BD" }, // 按需添加
            { "TH", "th-TH" },
            { "PH", "tl-PH" },
            { "VN", "vi-VN" },
            { "ID", "id-ID" },
            { "MY", "ms-MY" }, // { "SG", "ms-SG" }, // SG 已映射为 zh-SG
            { "IL", "he-IL" },
            { "IR", "fa-IR" },
            { "TR", "tr-TR" },
            { "UA", "uk-UA" },
            { "PK", "ur-PK" }
        };

        /// <summary>
        /// 根据国家代码获取对应的 BCP 47 语言标签
        /// </summary>
        /// <param name="countryCode">ISO 3166-1 国家代码，如 "US", "HK"</param>
        /// <returns>BCP 47 语言标签，如 "en-US", "zh-HK"；若未知则返回 "en-US"</returns>
        public static string GetLanguageForCountry(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return "en-US";

            return CountryToLanguageMap.TryGetValue(countryCode, out var lang) ? lang : "en-US";
        }
    }
}