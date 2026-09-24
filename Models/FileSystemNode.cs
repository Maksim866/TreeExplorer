using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TreeExplorer.Services;
using TreeExplorer.ViewModels;

namespace TreeExplorer.Models;

/// <summary>
/// Представляет узел файловой системы, который может быть файлом или папкой.
/// </summary>
public partial class FileSystemNode : ViewModelBase
{
    private readonly DirectoryLoaderService directoryLoaderService = new();

    /// <summary>
    /// Название файла или каталога
    /// </summary>
    [ObservableProperty]
    private string name = string.Empty;

    /// <summary>
    /// Полный путь к элементу в файловой системе
    /// </summary>
    [ObservableProperty]
    private string fullPath = string.Empty;

    /// <summary>
    /// Указывает, является ли узел каталогом.
    /// </summary>
    [ObservableProperty]
    private bool isDirectory;

    /// <summary>
    /// Иконка, представляющая элемент в интерфейсе.
    /// </summary>
    [ObservableProperty]
    private Bitmap? icon;

    /// <summary>
    /// Флаг совпадения с текущим поисковым запросом.
    /// </summary>
    [ObservableProperty]
    private bool isMatch;

    /// <summary>
    /// Указывает, загружено ли дочернее содержимое узла.
    /// </summary>
    [ObservableProperty]
    private bool isLoaded;

    /// <summary>
    /// Состояние раскрытия элемента в дереве TreeView.
    /// </summary>
    [ObservableProperty]
    private bool isExpanded;

    /// <summary>
    /// Дочерние узлы файловой системы.
    /// </summary>
    public ObservableCollection<FileSystemNode> Children { get; } = new();

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

    /// <summary>
    /// Асинхронно подгружает содержимое текущего каталога через сервис загрузки директорий.
    /// </summary>
    public async Task LoadChildrenAsync()
    {
        await directoryLoaderService.LoadChildrenAsync(this);
    }
    /// <summary>
    /// Автоматический обработчик смены состояния IsExpanded.
    /// </summary>
    partial void OnIsExpandedChanged(bool value)
    {
        // Когда пользователь раскрывает ветку (value == true) и узел ещё не был загружен
        if (value && !IsLoaded && IsDirectory)
        {
            _ = LoadChildrenAsync();
        }
    }
}
