// Services/LanguageMapper.cs
using System;
using System.Collections.Generic;

namespace OriginalPoster.Services;

public static class LanguageMapper
{
    private static readonly Dictionary<string, string> CountryToLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- 核心英语区 ---
        { "US", "en-US" }, { "GB", "en-GB" }, { "CA", "en-CA" }, { "AU", "en-AU" },
        { "NZ", "en-NZ" }, { "IE", "en-IE" },

        // --- 中文地区 ---
        { "CN", "zh-CN" }, { "HK", "zh-HK" }, { "TW", "zh-TW" }, { "SG", "zh-SG" },
        { "MO", "zh-MO" }, // 澳门

        // --- 东亚 ---
        { "JP", "ja-JP" }, 
        { "KR", "ko-KR" }, { "KP", "ko-KP" }, // 包含朝鲜（尽管内容很少）

        // --- 北欧 (Nordic Noir 罪案剧重镇) ---
        { "SE", "sv-SE" }, // 瑞典 (如: 维兰德, 桥)
        { "DK", "da-DK" }, // 丹麦 (如: 权力的堡垒)
        { "NO", "nb-NO" }, // 挪威 (如: 羞耻)
        { "FI", "fi-FI" }, // 芬兰
        { "IS", "is-IS" }, // 冰岛

        // --- 西欧/中欧 ---
        { "FR", "fr-FR" }, { "DE", "de-DE" }, { "ES", "es-ES" }, { "IT", "it-IT" },
        { "NL", "nl-NL" }, // 荷兰
        { "BE", "fr-BE" }, // 比利时 (官方双语，偏向法语或弗拉芒语，这里暂设法语)
        { "CH", "de-CH" }, // 瑞士 (偏向德语)
        { "AT", "de-AT" }, // 奥地利

        // --- 东欧/东南欧 (重要影视产地) ---
        { "RU", "ru-RU" }, 
        { "PL", "pl-PL" }, // 波兰 (重要电影产地)
        { "UA", "uk-UA" }, // 乌克兰
        { "CZ", "cs-CZ" }, // 捷克
        { "HU", "hu-HU" }, // 匈牙利
        { "RO", "ro-RO" }, // 罗马尼亚
        { "BG", "bg-BG" }, // 保加利亚
        { "GR", "el-GR" }, // 希腊

        // --- 热门剧集出口国 ---
        { "TR", "tr-TR" }, // 土耳其 (非常重要！肥皂剧出口大国)
        { "PT", "pt-PT" }, // 葡萄牙
        { "BR", "pt-BR" }, // 巴西

        // --- 拉丁美洲 (西班牙语变体) ---
        { "MX", "es-MX" }, // 墨西哥 (拉美影视中心)
        { "AR", "es-AR" }, // 阿根廷
        { "CO", "es-CO" }, // 哥伦比亚
        { "CL", "es-CL" }, // 智利
        { "PE", "es-PE" }, // 秘鲁
        { "VE", "es-VE" }, // 委内瑞拉

        // --- 中东 ---
        { "SA", "ar-SA" }, { "EG", "ar-EG" }, { "AE", "ar-AE" }, // 阿拉伯语系
        { "IL", "he-IL" }, // 以色列
        { "IR", "fa-IR" }, // 伊朗 (波斯语电影在国际获奖颇多)

        // --- 南亚/东南亚 ---
        { "IN", "hi-IN" }, // 印度 (宝莱坞)
        { "TH", "th-TH" }, // 泰国
        { "VN", "vi-VN" }, // 越南
        { "ID", "id-ID" }, // 印度尼西亚
        { "MY", "ms-MY" }, // 马来西亚
        { "PH", "tl-PH" }, // 菲律宾 (他加禄语)
        { "PK", "ur-PK" }  // 巴基斯坦
    };

    public static string GetLanguageForCountry(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return "en-US";
            
        // 1. 查字典
        if (CountryToLanguageMap.TryGetValue(countryCode, out var lang)) 
            return lang;

        // 2. 智能兜底 (利用系统自带的全球化信息)
        // 应对冷门国家 (如爱沙尼亚 EE, 立陶宛 LT 等)，避免直接回退到 en-US
        try 
        {
            var region = new System.Globalization.RegionInfo(countryCode);
            // RegionInfo 不直接提供 "语言-国家" 代码，但我们可以尝试猜测
            // 这里为了稳健，如果字典里没有，还是保守回退，或者你可以只返回 en-US
            // return region.TwoLetterISORegionName; 
        }
        catch {}

        return "en-US";
    }
}
