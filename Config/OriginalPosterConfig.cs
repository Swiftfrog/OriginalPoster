using Emby.Web.GenericEdit;
using System.ComponentModel;

namespace OriginalPoster.Config;

public class OriginalPosterConfig : EditableOptionsBase
{
    // 必须实现的抽象属性：配置页面标题
    public override string EditorTitle => "OriginalPoster Settings";

    // 用户可配置项
    [DisplayName("启用插件")]
    [Description("是否启用原生语言海报优先功能")]
    public bool EnablePlugin { get; set; } = true;

    [DisplayName("TMDB API Key")]
    [Description("请在 https://www.themoviedb.org/settings/api 获取")]
    public string TmdbApiKey { get; set; } = string.Empty;
}
