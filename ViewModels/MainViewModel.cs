using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using TreeExplorer.Models;

namespace TreeExplorer.ViewModels;

/// <summary>
/// Представляет ViewModel для главного окна приложения, управляющую корневыми узлами файловой системы.
/// </summary>
public class MainViewModel : ViewModelBase
{
    /// <summary>
    /// Текущий путь, выбранный пользователем в файловой системе.
    /// </summary>
    public string currentPath = string.Empty;

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
        var rootNode = new FileSystemNode(CurrentPath, isDirectory);

        if (isDirectory)
        {
            rootNode.LoadChildren();
        }

        RootNodes.Add(rootNode);
    }
}
