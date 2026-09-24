using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using TreeExplorer.Models;

namespace TreeExplorer.ViewModels;

/// <summary>
/// Представляет ViewModel для главного окна приложения, управляющую корневыми узлами файловой системы.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    /// <summary>
    /// Текущий путь, выбранный пользователем в файловой системе.
    /// </summary>
    private string currentPath = string.Empty;
    private string searchText = string.Empty;

    private bool isSearching;

    private FileSystemNode? originalRootNode;

    public string CurrentPath
    {
        get => currentPath;
        set
        {
            if (currentPath != value)
            {
                currentPath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Текст для поиска по названию файлов и папок.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (searchText != value)
            {
                searchText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Флаг выполнения поиска (для отключения кнопок или показа индикатора)
    /// </summary>
    public bool IsSearching
    {
        get => isSearching;
        set
        {
            if (isSearching != value)
            {
                isSearching = value;
                OnPropertyChanged();
            }
        }
    }

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

    public void OpenPath()
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
            originalRootNode.LoadChildren();
        }

        var task = ApplySearchAsync();
    }

    public async Task ApplySearchAsync()
    {
        if (IsSearching)
        {
            return;
        }

        IsSearching = true;

        try
        {
            // 1. Если поисковый запрос пуст — возвращаем исходную структуру текущей папки
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
            string targetPath = CurrentPath?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath))
            {
                return;
            }

            // 2. Выполняем поиск совпадений первого уровня в фоновом потоке
            var searchResultNode = await Task.Run(() =>
            {
                var rootDirNode = new FileSystemNode(targetPath, isDirectory: true)
                {
                    IsLoaded = true,
                    IsExpanded = true
                };
                rootDirNode.Children.Clear();

                var dirInfo = new DirectoryInfo(targetPath);

                // А) Ищем совпадения среди папок первого уровня
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

                            // Загружаем её дочерние элементы по умолчанию, 
                            // чтобы у узла появилось дерево и стрелочка для раскрытия:
                            folderNode.LoadChildren();

                            rootDirNode.Children.Add(folderNode);
                        }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (Exception) { }

                // Б) Ищем совпадения среди файлов первого уровня
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

            // 3. Вывод результатов в UI
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
    [RelayCommand]
    private void SetAsRoot(FileSystemNode? node)
    {
        if (node == null || !node.IsDirectory || string.IsNullOrWhiteSpace(node.FullPath) || !Directory.Exists(node.FullPath))
        {
            return;
        }

        SearchText = string.Empty;
        CurrentPath = node.FullPath;
        OpenPath();
    }
}
