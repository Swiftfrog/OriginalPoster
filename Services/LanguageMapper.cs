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
            // 阿拉伯语地区
            { "AE", "ar-AE" }, { "BH", "ar-BH" }, { "EG", "ar-EG" }, { "IQ", "ar-IQ" },
            { "JO", "ar-JO" }, { "LY", "ar-LY" }, { "MA", "ar-MA" }, { "QA", "ar-QA" },
            { "SA", "ar-SA" }, { "TD", "ar-TD" }, { "YE", "ar-YE" },

            // 欧洲
            { "BY", "be-BY" }, { "BG", "bg-BG" }, { "CZ", "cs-CZ" }, { "DK", "da-DK" },
            { "DE", "de-DE" }, { "AT", "de-AT" }, { "CH", "de-CH" },
            { "GR", "el-GR" }, { "CY", "el-CY" },
            { "GB", "en-GB" }, { "IE", "en-IE" },
            { "US", "en-US" }, { "CA", "en-CA" }, { "AU", "en-AU" },
            { "NZ", "en-NZ" }, { "ZA", "en-ZA" }, { "JM", "en-JM" }, { "AG", "en-AG" },
            { "BB", "en-BB" }, { "BZ", "en-BZ" }, { "CM", "en-CM" }, { "GH", "en-GH" },
            { "GI", "en-GI" }, { "GG", "en-GG" }, { "GY", "en-GY" }, { "KE", "en-KE" },
            { "LC", "en-LC" }, { "MW", "en-MW" }, { "PG", "en-PG" }, { "TC", "en-TC" },
            { "ZM", "en-ZM" }, { "ZW", "en-ZW" },
            { "ES", "es-ES" }, { "MX", "es-MX" }, { "AR", "es-AR" }, { "CL", "es-CL" },
            { "CO", "es-CO" }, { "DO", "es-DO" }, { "EC", "es-EC" }, { "GT", "es-GT" },
            { "HN", "es-HN" }, { "NI", "es-NI" }, { "PA", "es-PA" }, { "PE", "es-PE" },
            { "PY", "es-PY" }, { "SV", "es-SV" }, { "UY", "es-UY" }, { "GQ", "es-GQ" },
            { "FR", "fr-FR" }, { "CA", "fr-CA" }, { "BE", "fr-BE" }, { "CH", "fr-CH" },
            { "BF", "fr-BF" }, { "CD", "fr-CD" }, { "CI", "fr-CI" }, { "GF", "fr-GF" },
            { "GP", "fr-GP" }, { "MC", "fr-MC" }, { "ML", "fr-ML" }, { "MU", "fr-MU" },
            { "PF", "fr-PF" },
            { "IT", "it-IT" }, { "VA", "it-VA" },
            { "NL", "nl-NL" }, { "BE", "nl-BE" },
            { "PL", "pl-PL" }, { "PT", "pt-PT" }, { "BR", "pt-BR" }, { "AO", "pt-AO" },
            { "MZ", "pt-MZ" },
            { "RO", "ro-RO" }, { "MD", "ro-MD" },
            { "RU", "ru-RU" },
            { "SE", "sv-SE" }, { "NO", "no-NO" }, { "DK", "da-DK" }, { "FI", "fi-FI" },
            { "HU", "hu-HU" }, { "HR", "hr-HR" }, { "SK", "sk-SK" }, { "SI", "sl-SI" },
            { "LV", "lv-LV" }, { "LT", "lt-LT" }, { "EE", "et-EE" },
            { "AL", "sq-AL" }, { "XK", "sq-XK" }, { "ME", "sr-ME" }, { "RS", "sr-RS" },

            // 亚洲
            { "CN", "zh-CN" }, { "HK", "zh-HK" }, { "TW", "zh-TW" }, { "SG", "zh-SG" },
            { "JP", "ja-JP" },
            { "KR", "ko-KR" },
            { "IN", "hi-IN" }, { "BD", "bn-BD" }, { "PK", "ur-PK" },
            { "TH", "th-TH" },
            { "PH", "tl-PH" },
            { "VN", "vi-VN" },
            { "ID", "id-ID" },
            { "MY", "ms-MY" }, { "SG", "ms-SG" },
            { "IL", "he-IL" },
            { "IR", "fa-IR" },
            { "TR", "tr-TR" },
            { "GE", "ka-GE" }, { "KZ", "kk-KZ" }, { "KG", "ky-KG" },
            { "UA", "uk-UA" },
            { "UZ", "uz-UZ" },
            { "NP", "ne-NP" }, { "LK", "si-LK" }, { "TZ", "sw-TZ" },

            // 其他
            { "AD", "ca-AD" }, { "ES", "ca-ES" },
            { "FR", "br-FR" }, { "FR", "oc-FR" },
            { "IE", "ga-IE" }, { "GB", "gd-GB" }, { "ES", "gl-ES" },
            { "FR", "eu-ES" } // 巴斯克语归属西班牙/法国
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