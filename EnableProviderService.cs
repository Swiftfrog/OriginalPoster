using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using System;
using System.Linq;

namespace OriginalPoster
{
    /// <summary>
    /// 为所有电影库启用 OriginalPoster Provider
    /// 访问: http://your-emby:8096/emby/OriginalPoster/Enable
    /// </summary>
    [Route("/OriginalPoster/Enable", "POST", Summary = "Enable OriginalPoster for all movie libraries")]
    [Authenticated(Roles = "Admin")]
    public class EnableProviderRequest : IReturnVoid
    {
    }

    public class EnableProviderResponse
    {
        public string Message { get; set; }
        public int LibrariesUpdated { get; set; }
    }

    public class EnableProviderService : IService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public EnableProviderService(
            ILibraryManager libraryManager,
            ILogManager logManager)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger("EnableProviderService");
        }

        public object Post(EnableProviderRequest request)
        {
            _logger.Info("╔═══════════════════════════════════════════════════════════");
            _logger.Info("║ ENABLE PROVIDER API CALLED");
            _logger.Info("╚═══════════════════════════════════════════════════════════");

            try
            {
                int updated = 0;

                // 获取所有虚拟文件夹（媒体库）
                var virtualFolders = _libraryManager.GetVirtualFolders();
                
                _logger.Info("Found {0} virtual folders", virtualFolders.Length);

                foreach (var folder in virtualFolders)
                {
                    _logger.Info("Checking folder: {0} (Type: {1})", 
                        folder.Name, 
                        folder.CollectionType ?? "mixed");

                    // 只处理电影库
                    if (folder.CollectionType != "movies")
                        continue;

                    _logger.Info("Processing movie library: {0}", folder.Name);

                    // 获取库选项
                    var options = folder.LibraryOptions;
                    if (options == null)
                    {
                        _logger.Warn("Library options is null for {0}", folder.Name);
                        continue;
                    }

                    // 检查 DisabledImageFetchers
                    if (options.DisabledImageFetchers == null)
                    {
                        options.DisabledImageFetchers = new string[0];
                    }

                    // 从禁用列表中移除 OriginalPoster
                    var disabled = options.DisabledImageFetchers.ToList();
                    var wasDisabled = disabled.Remove("OriginalPoster");

                    if (wasDisabled)
                    {
                        _logger.Info("✓ Removed OriginalPoster from disabled list");
                        options.DisabledImageFetchers = disabled.ToArray();
                    }
                    else
                    {
                        _logger.Info("OriginalPoster was not in disabled list");
                    }

                    // 确保在启用列表中（如果有这个字段）
                    if (options.ImageFetchers != null)
                    {
                        var fetchers = options.ImageFetchers.ToList();
                        if (!fetchers.Contains("OriginalPoster"))
                        {
                            fetchers.Insert(0, "OriginalPoster"); // 添加到最前面
                            options.ImageFetchers = fetchers.ToArray();
                            _logger.Info("✓ Added OriginalPoster to enabled list");
                        }
                    }

                    // 保存更改
                    _libraryManager.UpdateVirtualFolderOptions(folder.ItemId, options);
                    _logger.Info("✓ Saved library options for: {0}", folder.Name);
                    updated++;
                }

                var message = updated > 0 
                    ? $"Successfully enabled OriginalPoster for {updated} movie librar{(updated == 1 ? "y" : "ies")}. Please refresh your movie metadata."
                    : "No movie libraries found or OriginalPoster was already enabled.";

                _logger.Info("═══════════════════════════════════════════════════════════");
                _logger.Info(message);

                return new EnableProviderResponse
                {
                    Message = message,
                    LibrariesUpdated = updated
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorException("ERROR in Enable Provider API", ex);
                return new EnableProviderResponse
                {
                    Message = $"Error: {ex.Message}",
                    LibrariesUpdated = 0
                };
            }
        }
    }
}
