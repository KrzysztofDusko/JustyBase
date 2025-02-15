using JustyBase.Models;
using System;
using System.Globalization;

namespace JustyBase.Converters;

//https://github.com/KrzysztofDusko/JustDataEvoProject/issues/121
internal sealed class ForGroupValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value.ToString() == (object)TableRow.EMPTY_NAME_PLACEHOLDED)
        {
            return "<NULL>";
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}