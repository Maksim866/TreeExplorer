using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TreeExplorer.Models;
using TreeExplorer.ViewModels;

namespace TreeExplorer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Обработчик кнопки «Перейти»
    /// </summary>
    private void OnOpenPathClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OpenPath();
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
            Title = "Выберите папку для просмотра",
            AllowMultiple = false
        };

        if (DataContext is MainViewModel currentVm &&
            !string.IsNullOrWhiteSpace(currentVm.CurrentPath) &&
            System.IO.Directory.Exists(currentVm.CurrentPath))
        {
            try
            {
                var startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(currentVm.CurrentPath);
                if (startFolder != null)
                {
                    // Исправлено имя свойства: SuggestedStartLocation
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
                vm.OpenPath();
            }
        }
    }

    /// <summary>
    /// Обработчик события разворачивания узла дерева.
    /// </summary>
    private void OnTreeExpanded(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control control && control.DataContext is FileSystemNode node)
        {
            node.LoadChildren();
        }
    }

    private void OnTreeItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TreeView treeView && treeView.SelectedItem is FileSystemNode selectedNode)
        {
            // Если это файл — открываем его системной программой по умолчанию
            if (!selectedNode.IsDirectory && File.Exists(selectedNode.FullPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = selectedNode.FullPath,
                        UseShellExecute = true // Позволяет ОС открыть файл через ассоциацию по умолчанию
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при открытии файла: {ex.Message}");
                }
            }
        }
    }
}
