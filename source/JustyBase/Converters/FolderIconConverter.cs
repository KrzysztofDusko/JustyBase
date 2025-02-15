using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace JustyBase.Converters;

public sealed class FolderIconConverter(Bitmap file, Bitmap folderExpanded, Bitmap folderCollapsed) : IMultiValueConverter
{
    private readonly Bitmap _file = file;
    private readonly Bitmap _folderExpanded = folderExpanded;
    private readonly Bitmap _folderCollapsed = folderCollapsed;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2 &&
            values[0] is bool isDirectory &&
            values[1] is bool isExpanded)
        {
            if (!isDirectory)
                return _file;
            else
                return isExpanded ? _folderExpanded : _folderCollapsed;
        }

        return null;
    }
}
