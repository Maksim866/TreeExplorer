using System;
using System.IO;
using System.Threading.Tasks;

namespace TreeExplorer.Services;

/// <summary>
/// Сервис для работы с файловой ситемой (сканирование, поиск, загрузка содержимого).
/// </summary>
public class FileOpenerService
{
    /// <summary>
    /// Асинхронное открывает системный файл по умолчанию через оболочку ОС.
    /// </summary>
    public Task OpenFileWithDefaultProgramAsync(string filePath)
    {
        return Task.Run(() =>
        {
            if (File.Exists(filePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка открытия файла: {ex.Message}");
                }
            }
        });
    }
}
