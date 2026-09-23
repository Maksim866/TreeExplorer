using System;
using System.IO;
using System.Runtime.InteropServices;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace TreeExplorer.Services;

/// <summary>
/// Предоставляет методы для получения системных иконок Windows для файлов и папок.
/// </summary>
public static class WindowsFileIconProvider
{

    /// <summary>
    /// Структура для хранения информации о файле, полученной из Windows Shell.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    // Флаги оболочки Windows
    private const uint SHGFI_ICON = 0x000000100;     // Получить иконку
    private const uint SHGFI_LARGEICON = 0x000000000; // 32x32 пикселя
    private const uint SHGFI_SMALLICON = 0x000000001; // 16x16 пикселей
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010; // Не проверять физическое наличие файла

    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    /// <summary>
    /// Извлекает информацию о файле или папке, включая иконку, используя Windows Shell.
    /// </summary>
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    /// <summary>
    /// Освобождает дескриптор иконки, полученный из Windows Shell.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Получает системную иконку Windows для файла или папки.
    /// </summary>
    public static AvaloniaBitmap? GetIcon(string path, bool isFolder, bool largeIcon = false)
    {
        // Проверяем, что ОС — Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        SHFILEINFO shfi = new SHFILEINFO();
        uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
        flags |= largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON;

        uint attributes = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

        IntPtr res = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

        if (res == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            // Преобразуем Win32 HICON в Bitmap для Avalonia через создание System.Drawing.Icon
            using (var icon = System.Drawing.Icon.FromHandle(shfi.hIcon))
            using (var ms = new MemoryStream())
            {
                // Сохраняем в PNG-поток
                using (var bitmap = icon.ToBitmap())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                }
                ms.Position = 0;
                return new AvaloniaBitmap(ms); // Создаем Bitmap для Avalonia
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            DestroyIcon(shfi.hIcon); // Обязательно освобождаем память дескриптора
        }
    }
}
