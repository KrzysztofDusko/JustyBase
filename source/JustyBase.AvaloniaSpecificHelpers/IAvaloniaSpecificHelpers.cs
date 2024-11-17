using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace JustyBase.Services;

public interface IAvaloniaSpecificHelpers
{
    void CloseMainWindow();
    IClipboard? GetClipboard();
    IStorageProvider? GetStorageProvider();
    Window? GetMainWindow();
    Task CopyFileToClipboard(string path);
}