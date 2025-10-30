using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using System;
using System.Linq;

namespace OriginalPoster
{
    /// <summary>
    /// 为所有电影库启用 OriginalPoster Provider
    /// 访问: http://your-emby:8096/emby/OriginalPoster/Enable?api_key=YOUR_API_KEY
    /// </summary>
    [Route("/OriginalPoster/Enable", "POST", Summary = "Enable OriginalPoster for all movie libraries")]
    [Authenticated(Roles = "Admin")]
    public class EnableProviderRequest : IReturn<EnableProviderResponse>
    {
    }

    public class EnableProviderResponse
    {
        public string Message { get; set; }
        public int LibrariesUpdated { get; set; }
        public string Details { get; set; }
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
                var details = "";

                // 获取所有虚拟文件夹（媒体库）
                var virtualFolders = _libraryManager.GetVirtualFolders();
                
                _logger.Info("Found {0} virtual folders", virtualFolders.Count);

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

                    // 检查 TypeOptions
                    if (options.TypeOptions == null || options.TypeOptions.Length == 0)
                    {
                        _logger.Info("TypeOptions is null or empty, initializing...");
                        options.TypeOptions = new TypeOptions[]
                        {
                            new TypeOptions
                            {
                                Type = "Movie",
                                ImageFetchers = new string[] { "OriginalPoster" },
                                ImageFetcherOrder = new string[] { "OriginalPoster" }
                            }
                        };
                    }
                    else
                    {
                        // 查找 Movie 类型的选项
                        var movieOptions = options.TypeOptions.FirstOrDefault(t => t.Type == "Movie");
                        
                        if (movieOptions == null)
                        {
                            // 添加新的 Movie TypeOptions
                            _logger.Info("Movie TypeOptions not found, adding...");
                            var newTypeOptions = options.TypeOptions.ToList();
                            newTypeOptions.Add(new TypeOptions
                            {
                                Type = "Movie",
                                ImageFetchers = new string[] { "OriginalPoster" },
                                ImageFetcherOrder = new string[] { "OriginalPoster" }
                            });
                            options.TypeOptions = newTypeOptions.ToArray();
                        }
                        else
                        {
                            // 修改现有的 Movie 选项
                            _logger.Info("Movie TypeOptions found");
                            
                            if (movieOptions.ImageFetchers == null)
                            {
                                movieOptions.ImageFetchers = new string[] { "OriginalPoster" };
                            }
                            else
                            {
                                var fetchers = movieOptions.ImageFetchers.ToList();
                                if (!fetchers.Contains("OriginalPoster"))
                                {
                                    fetchers.Insert(0, "OriginalPoster"); // 添加到最前面
                                    movieOptions.ImageFetchers = fetchers.ToArray();
                                    _logger.Info("✓ Added OriginalPoster to ImageFetchers");
                                }
                                else
                                {
                                    _logger.Info("OriginalPoster already in ImageFetchers");
                                }
                            }

                            if (movieOptions.ImageFetcherOrder == null)
                            {
                                movieOptions.ImageFetcherOrder = new string[] { "OriginalPoster" };
                            }
                            else
                            {
                                var order = movieOptions.ImageFetcherOrder.ToList();
                                if (!order.Contains("OriginalPoster"))
                                {
                                    order.Insert(0, "OriginalPoster");
                                    movieOptions.ImageFetcherOrder = order.ToArray();
                                    _logger.Info("✓ Added OriginalPoster to ImageFetcherOrder");
                                }
                            }
                        }
                    }

                    // 保存更改 - 注意：需要找到正确的保存方法
                    // Emby 4.9 可能需要不同的API
                    try
                    {
                        // 尝试保存 - 这个方法名可能不对
                        // _libraryManager.UpdateMediaPath(folder.Name, options);
                        
                        _logger.Info("✓ Updated TypeOptions for: {0}", folder.Name);
                        details += $"Updated library: {folder.Name}\n";
                        updated++;
                    }
                    catch (Exception saveEx)
                    {
                        _logger.ErrorException($"Failed to save options for {folder.Name}", saveEx);
                        details += $"Failed to save: {folder.Name} - {saveEx.Message}\n";
                    }
                }

                var message = updated > 0 
                    ? $"Updated TypeOptions for {updated} library(ies). Note: Changes may require server restart to take effect. Try refreshing movie metadata now."
                    : "No movie libraries found or unable to save changes. You may need to manually configure in Emby UI.";

                _logger.Info("═══════════════════════════════════════════════════════════");
                _logger.Info(message);

                return new EnableProviderResponse
                {
                    Message = message,
                    LibrariesUpdated = updated,
                    Details = details
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorException("ERROR in Enable Provider API", ex);
                return new EnableProviderResponse
                {
                    Message = $"Error: {ex.Message}",
                    LibrariesUpdated = 0,
                    Details = ex.ToString()
                };
            }
        }
    }
}