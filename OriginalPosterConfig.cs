// Pluginconfiguration.cs
using Emby.Web.GenericEdit;
using MediaBrowser.Model.Plugins;
using System.ComponentModel;

namespace OriginalPoster
{
    /// <summary>
    /// 插件配置类 - 使用 DisplayName 和 Description 特性来生成友好的 UI
    /// </summary>
    public class OriginalPosterConfig : EditableOptionsBase
    {
        
        public override string EditorTitle => "OriginalPoster Settings";
        
        /// <summary>
        /// 是否启用插件
        /// </summary>
        [DisplayName("启用插件")]
        [Description("是否启用 TMDB 原语言海报功能")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// TMDB API 密钥
        /// </summary>
        [DisplayName("TMDB API KEY")]
        [Description("在 https://www.themoviedb.org/settings/api 获取")]
        public string TmdbApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// 海报选择策略
        /// </summary>
        [DisplayName("海报选择策略")]
        [Description("选择原语言海报时的优先级策略")]
        public PosterSelectionStrategy PosterSelectionStrategy { get; set; } = PosterSelectionStrategy.OriginalLanguageFirst;
        
        /// <summary>
        /// 测试模式 - 第一阶段使用，返回测试数据
        /// </summary>
        [DisplayName("测试模式")]
        [Description("启用后将返回测试海报，用于验证插件功能")]
        public bool TestMode { get; set; } = true;
        
        /// <summary>
        /// 调试日志
        /// </summary>
        [DisplayName("调试日志")]
        [Description("启用后将在控制台输出详细的调试信息")]
        public bool DebugLogging { get; set; } = true;
        
        /// <summary>
        /// 测试用的海报URL（第一阶段使用）
        /// </summary>
        [DisplayName("测试海报 URL")]
        [Description("测试模式下使用的海报图片地址")]
        public string TestPosterUrl { get; set; } = "https://image.tmdb.org/t/p/original/cgZjpqRQt9sk6XMCwZ3B1NPAaoy.jpg";
        
    }

    /// <summary>
    /// 海报选择策略枚举
    /// </summary>
    public enum PosterSelectionStrategy
    {
        /// <summary>
        /// 优先原语言（即使评分较低）
        /// </summary>
        [Description("优先原语言")]
        OriginalLanguageFirst,
    
        /// <summary>
        /// 优先高评分（可能选到 null 无文字海报）
        /// </summary>
        [Description("优先高评分")]
        HighestRatingFirst
    }

}