using MediaBrowser.Model.Serialization;
using System.IO;

namespace OriginalPoster.Configuration
{
    public static class ConfigurationFactory
    {
        public static T LoadConfiguration<T>(string configPath, IXmlSerializer xmlSerializer) where T : new()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    return xmlSerializer.DeserializeFromFile<T>(configPath);
                }
                catch
                {
                    // 如果加载失败，返回默认配置
                }
            }
            return new T();
        }

        public static void SaveConfiguration<T>(string configPath, T config, IXmlSerializer xmlSerializer)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            xmlSerializer.SerializeToFile(config, configPath);
        }
    }
}
