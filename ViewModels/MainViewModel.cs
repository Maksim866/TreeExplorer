using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TreeExplorer.Models;
using TreeExplorer.Services;

namespace TreeExplorer.ViewModels;

/// <summary>
/// Представляет ViewModel для главного окна приложения, управляющую корневыми узлами файловой системы.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly FileSearchService fileSearchService = new();
    private readonly FileOpenerService fileOpenerService = new();

    private FileSystemNode? originalRootNode;

    /// <summary>
    /// Текущий путь, выбранный пользователем в файловой системе.
    /// </summary>
    [ObservableProperty]
    private string currentPath = string.Empty;

    /// <summary>
    /// Текст поискового запроса
    /// </summary>
    [ObservableProperty]
    private string searchText = string.Empty;

    /// <summary>
    /// Флаг активности фонового поиска
    /// </summary>
    [ObservableProperty]
    private bool isSearching;

    /// <summary>
    /// Корневые узлы файловой системы, представляющие доступные диски.
    /// </summary>
    public ObservableCollection<FileSystemNode> RootNodes { get; } = new();

    /// <summary>
    /// Инициализирует новый экземпляр MainViewModel и загружает корневые диски файловой системы.
    /// </summary>
    public MainViewModel()
    {
        // При старте ничего не заполнено — ждем действий пользователя
    }

    /// <summary>
    /// Асинхронно открывает каталог по текущему пути и применяет поиск
    /// </summary>
    [RelayCommand]
    public async Task OpenPathAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath) || (!Directory.Exists(CurrentPath) && !File.Exists(CurrentPath)))
        {
            return;
        }

        RootNodes.Clear();

        bool isDirectory = Directory.Exists(CurrentPath);
        originalRootNode = new FileSystemNode(CurrentPath, isDirectory)
        {
            IsExpanded = true
        };

        if (isDirectory)
        {
            await originalRootNode.LoadChildrenAsync();
        }

        await ApplySearchAsync();
    }

    /// <summary>
    /// Асинхронно выполняет фильтрацию элементов в пределах текущего каталога
    /// </summary>
    [RelayCommand]
    public async Task ApplySearchAsync()
    {
        if (IsSearching)
        {
            return;
        }

        IsSearching = true;

        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                RootNodes.Clear();
                if (originalRootNode != null)
                {
                    RootNodes.Add(originalRootNode);
                }
                return;
            }

            string query = SearchText.Trim();
            string targetPath = CurrentPath.Trim();

            var searchResultNode = await fileSearchService.SearchFirstLevelAsync(targetPath, query);

            RootNodes.Clear();
            if (searchResultNode != null)
            {
                RootNodes.Add(searchResultNode);
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Команда для установки выбранного узла в качестве текущего корневого каталога.
    /// </summary>
    /// <param name="node"></param>
    [RelayCommand]
    private async Task SetAsRootAsync(FileSystemNode? node)
    {
        if (node == null || !node.IsDirectory || string.IsNullOrWhiteSpace(node.FullPath) || !Directory.Exists(node.FullPath))
        {
            return;
        }

        SearchText = string.Empty;
        CurrentPath = node.FullPath;
        await OpenPathAsync();
    }

    /// <summary>
    /// Команда для открытия файла внешней программой по умолчанию
    /// </summary>
    [RelayCommand]
    private async Task OpenFileAsync(FileSystemNode? node)
    {
        if (node != null && !node.IsDirectory && !string.IsNullOrWhiteSpace(node.FullPath))
        {
            await fileOpenerService.OpenFileWithDefaultProgramAsync(node.FullPath);
        }
    }
}
