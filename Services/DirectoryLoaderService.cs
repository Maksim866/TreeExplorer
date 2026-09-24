using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TreeExplorer.Models;

namespace TreeExplorer.Services;

public class DirectoryLoaderService
{

    /// <summary>
    /// Асинхронно загружает папки и файлы для указанного узла-директории.
    /// </summary>
    public async Task LoadChildrenAsync(FileSystemNode parentNode)
    {
        if (parentNode == null || !parentNode.IsDirectory || parentNode.IsLoaded)
        {
            return;
        }

        var children = await Task.Run(() =>
        {
            var result = new List<FileSystemNode>();
            try
            {
                var directoryInfo = new DirectoryInfo(parentNode.FullPath);

                foreach (var dir in directoryInfo.GetDirectories())
                {
                    if ((dir.Attributes & FileAttributes.Hidden) != 0)
                    {
                        continue;
                    }
                    result.Add(new FileSystemNode(dir.FullName, isDirectory: true));
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    if ((file.Attributes & FileAttributes.Hidden) != 0)
                    {
                        continue;
                    }
                    result.Add(new FileSystemNode(file.FullName, isDirectory: false));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }

            return result;
        });

        parentNode.Children.Clear();

        foreach (var child in children)
        {
            parentNode.Children.Add(child);
        }

        parentNode.IsLoaded = true;
    }
}
