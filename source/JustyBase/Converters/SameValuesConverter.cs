using System;
using System.Collections.Generic;
using System.Globalization;

namespace JustyBase.Converters;

//result grid, row view
public sealed class SameValuesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(IBrush) && (value is null || value is not List<string>))
        {
            return Brushes.Transparent;
        }
        if (targetType == typeof(double) && (value is null || value is not List<string>))
        {
            return 1.0;
        }
        bool res = true;

        if (value is List<string> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (list[i] != list[i + 1])
                {
                    res = false;
                    break;
                }
            }
        }

        if (targetType == typeof(IBrush))
        {
            if (!res)
            {
                return App.Current.FindResource("SystemAccentColorBrush");
            }
            return Avalonia.Media.Brushes.Transparent;
        }
        else if (targetType == typeof(double))
        {
            if (!res)
            {
                return (double)App.Current.FindResource("ListAccentMediumOpacity");
            }
            return 1.0;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

