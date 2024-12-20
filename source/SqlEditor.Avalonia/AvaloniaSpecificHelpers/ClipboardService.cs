﻿using Avalonia.Input.Platform;
using JustyBase.PluginCommon.Contracts;
using JustyBase.Services;
using System.Data;
using System.Threading.Tasks;

namespace SqlEditor.Avalonia.AvaloniaSpecificHelpers;

public sealed class ClipboardService : IClipboardService
{

    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private IClipboard? _clipboard;
    private IClipboard? Clipboard => _clipboard ??= _avaloniaSpecificHelpers.GetClipboard();

    public ClipboardService(IAvaloniaSpecificHelpers avaloniaSpecificHelpers)
    {
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
    }
    public async Task<object?> GetDataAsync(string format)
    {
        if (Clipboard is null)
        {
            return null;
        }
        return await Clipboard?.GetDataAsync(format);
    }

    public async Task<string[]> GetFormatsAsync()
    {
        return await Clipboard.GetFormatsAsync();
    }

    public async Task<string> GetTextAsync()
    {
        return await Clipboard.GetTextAsync();
    }

    public async Task SetTextAsync(string txt)
    {
        await Clipboard.SetTextAsync(txt);
    }
}
