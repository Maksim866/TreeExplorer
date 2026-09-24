using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TreeExplorer.Constants;
using TreeExplorer.Models;
using TreeExplorer.Services;
using TreeExplorer.ViewModels;

namespace TreeExplorer.Views;

/// <summary>
/// Класс логики представления для главного окна
/// </summary>
public partial class MainWindow : Window
{
    private readonly FileOpenerService fileOpenerService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Обработчик нажатия кнопки поиска
    /// </summary>
    private async void OnSearchButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.ApplySearchAsync();
        }
    }

    /// <summary>
    /// Обработчик кнопки «Перейти»
    /// </summary>
    private async void OnOpenPathClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.OpenPathAsync();
        }
    }

    /// <summary>
    /// Обработчик кнопки «Обзор...» для выбора папки
    /// </summary>
    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = AppConstants.FolderPickerTitle,
            AllowMultiple = false
        };

        if (DataContext is MainViewModel currentVm &&
            !string.IsNullOrWhiteSpace(currentVm.CurrentPath) &&
            Directory.Exists(currentVm.CurrentPath))
        {
            try
            {
                var startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(currentVm.CurrentPath);
                if (startFolder != null)
                {
                    options.SuggestedStartLocation = startFolder;
                }
            }
            catch
            {
                // Игнорируем ошибки при получении начальной папки
            }
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

        if (folders.Count > 0 && DataContext is MainViewModel vm)
        {
            var selectedFolder = folders[0];

            string localPath = string.Empty;

            // Надежный способ извлечения пути для Windows (включая кириллицу)
            if (selectedFolder.Path.IsAbsoluteUri)
            {
                localPath = Uri.UnescapeDataString(selectedFolder.Path.LocalPath);
            }
            else
            {
                localPath = selectedFolder.Path.ToString();
            }

            if (!string.IsNullOrWhiteSpace(localPath))
            {
                // Устанавливаем путь и запускаем построение дерева
                vm.CurrentPath = localPath;
                await vm.OpenPathAsync();
            }
        }
    }

    /// <summary>
    /// Обработчик двойного клика по элементу дерева (открытие файла)
    /// </summary>
    private async void OnTreeItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TreeView treeView && treeView.SelectedItem is FileSystemNode selectedNode)
        {
            if (!selectedNode.IsDirectory)
            {
                await fileOpenerService.OpenFileWithDefaultProgramAsync(selectedNode.FullPath);
            }
        }
    }
}
