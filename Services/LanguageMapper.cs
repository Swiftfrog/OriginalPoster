// Services/LanguageMapper.cs
using System;
using System.Collections.Generic;

namespace OriginalPoster.Services
{
    /// <summary>
    /// 国家代码 (ISO 3166-1) 到语言代码 (ISO 639-1) 的映射器
    /// 用于根据电影制片国家自动选择原语言
    /// </summary>
    public static class LanguageMapper
    {
        /// <summary>
        /// 静态映射表：国家代码 -> 语言代码
        /// 覆盖主要电影生产国
        /// </summary>
        private static readonly Dictionary<string, string> CountryToLanguageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // 英语国家
            { "US", "en" }, { "GB", "en" }, { "CA", "en" }, { "AU", "en" }, { "NZ", "en" },
            // 中文地区
            { "CN", "zh" }, { "TW", "zh" }, { "HK", "zh" },
            // 东亚
            { "JP", "ja" }, { "KR", "ko" },
            // 欧洲
            { "FR", "fr" }, { "DE", "de" }, { "ES", "es" }, { "IT", "it" }, { "RU", "ru" },
            { "PT", "pt" }, { "NL", "nl" }, { "SE", "sv" }, { "NO", "no" }, { "DK", "da" },
            { "FI", "fi" }, { "PL", "pl" }, { "CZ", "cs" }, { "HU", "hu" }, { "GR", "el" },
            { "TR", "tr" },
            // 中东/南亚
            { "SA", "ar" }, { "AE", "ar" }, { "EG", "ar" }, { "IL", "he" }, { "IR", "fa" },
            { "IN", "hi" }, { "TH", "th" }
        };

        /// <summary>
        /// 根据国家代码获取对应的语言代码
        /// </summary>
        /// <param name="countryCode">ISO 3166-1 国家代码，如 "US", "CN"</param>
        /// <returns>ISO 639-1 语言代码，如 "en", "zh"；若未知则返回 "en"</returns>
        public static string GetLanguageForCountry(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return "en";

            return CountryToLanguageMap.TryGetValue(countryCode, out var lang) ? lang : "en";
        }
    }
}
