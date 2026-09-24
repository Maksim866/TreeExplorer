using System;
using System.IO;
using System.Threading.Tasks;
using TreeExplorer.Models;

namespace TreeExplorer.Services;

public class FileSearchService
{

    /// <summary>
    /// Асинхронно выполняет поверхностный поиск совпадений в заданной директории
    /// </summary>
    public async Task<FileSystemNode?> SearchFirstLevelAsync(string targetPath, string query)
    {
        if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath))
        {
            return null;
        }

        return await Task.Run(() =>
        {
            var rootDirNode = new FileSystemNode(targetPath, isDirectory: true)
            {
                IsLoaded = true,
                IsExpanded = true
            };
            rootDirNode.Children.Clear();

            var dirInfo = new DirectoryInfo(targetPath);

            // 1. Поиск подпапок
            try
            {
                foreach (var subDir in dirInfo.GetDirectories())
                {
                    if (subDir.Attributes.HasFlag(FileAttributes.Hidden) ||
                        subDir.Attributes.HasFlag(FileAttributes.System))
                    {
                        continue;
                    }

                    if (subDir.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        var folderNode = new FileSystemNode(subDir.FullName, isDirectory: true)
                        {
                            IsMatch = true
                        };

                        rootDirNode.Children.Add(folderNode);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }

            // 2. Поиск файлов
            try
            {
                foreach (var file in dirInfo.GetFiles())
                {
                    if (file.Attributes.HasFlag(FileAttributes.Hidden) ||
                        file.Attributes.HasFlag(FileAttributes.System))
                    {
                        continue;
                    }

                    if (file.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        var fileNode = new FileSystemNode(file.FullName, isDirectory: false)
                        {
                            IsMatch = true
                        };
                        fileNode.Children.Clear();
                        rootDirNode.Children.Add(fileNode);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }

            return rootDirNode;
        });
    }
}
