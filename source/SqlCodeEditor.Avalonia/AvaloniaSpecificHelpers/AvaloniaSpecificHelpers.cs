using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace JustyBase.Services;

public sealed class AvaloniaSpecificHelpers : IAvaloniaSpecificHelpers
{
    public IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window.StorageProvider;
        }
        return null;
    }

    public IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window.Clipboard;
        }
        return null;
    }

    public void CloseMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            window.Close();
        }
    }

    public Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window;
        }
        return null;
    }

    public async Task CopyFileToClipboard(string path)
    {
        if (path is not null && File.Exists(path))
        {
            var clipboard = GetClipboard();
            if (clipboard is null)
            {
                return;
            }
            if (GetStorageProvider() is IStorageProvider sp && await sp.TryGetFileFromPathAsync(path) is IStorageFile fl)
            {
                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Files, new IStorageItem[] { fl });
                await clipboard.SetDataObjectAsync(dataObject);
            }
        }
    }
}
