using MediaBrowser.Model.Plugins;

namespace EmbyOriginalPosterPlugin
{
    /// <summary>
    /// 插件配置类 - 第一阶段最简配置
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// 是否启用插件
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// 测试模式 - 第一阶段使用，返回测试数据
        /// </summary>
        public bool TestMode { get; set; }
        
        /// <summary>
        /// 调试日志
        /// </summary>
        public bool DebugLogging { get; set; }
        
        /// <summary>
        /// 测试用的海报URL（第一阶段使用）
        /// </summary>
        public string TestPosterUrl { get; set; }
        
        public PluginConfiguration()
        {
            // 默认值
            Enabled = true;
            TestMode = true;  // 第一阶段默认开启测试模式
            DebugLogging = true;  // 第一阶段默认开启调试日志
            
            // 测试用海报 - 使用TMDB的一个示例海报
            // 这是《肖申克的救赎》的中文海报，用于测试
            TestPosterUrl = "https://image.tmdb.org/t/p/original/zGINvGjdlO6TJRu9wESQvWlOKVT.jpg";
        }
    }
}
