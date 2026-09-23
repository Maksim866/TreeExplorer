using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using TreeExplorer.Services;

namespace TreeExplorer.Models;

/// <summary>
/// Представляет узел файловой системы, который может быть файлом или папкой.
/// </summary>
public class FileSystemNode
{
    /// <summary>
    /// Имя файла или папки.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Полный путь к файлу или папке.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Указывает, является ли узел директорией.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Иконка, представляющая файл или папку.
    /// </summary>
    public Bitmap? Icon { get; set; }

    /// <summary>
    /// Коллекция дочерних узлов, если узел является директорией.
    /// </summary>
    public ObservableCollection<FileSystemNode> Children { get; set; } = new();

    /// <summary>
    /// Указывает, загружены ли дочерние узлы для данного узла.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр узла файловой системы.
    /// </summary>
    public FileSystemNode(string path, bool isDirectory)
    {
        FullPath = path;
        IsDirectory = isDirectory;
        Name = Path.GetFileName(path);

        if (string.IsNullOrEmpty(Name))
        {
            Name = path;
        }

        Icon = WindowsFileIconProvider.GetIcon(path, isDirectory);

        if (isDirectory)
        {
            Children.Add(null!);
        }
    }

    public void LoadChildren()
    {
        if (!IsDirectory || IsLoaded)
        {
            return;
        }
        Children.Clear();
        try
        {
            var directories = new DirectoryInfo(FullPath);

            foreach (var dir in directories.GetDirectories())
            {
                if ((dir.Attributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }
                Children.Add(new FileSystemNode(dir.FullName, isDirectory: true));
            }

            foreach (var file in directories.GetFiles())
            {
                if ((file.Attributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }

                Children.Add(new FileSystemNode(file.FullName, isDirectory: false));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Игнорируем папки, к которым нет доступа
        }
        catch (Exception ex)
        {
            // Игнорируем ошибки чтения диска
        }
        IsLoaded = true;
    }

}
