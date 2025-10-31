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
}