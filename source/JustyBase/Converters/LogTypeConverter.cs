using JustyBase.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace JustyBase.Converters;

public sealed class LogTypeConverter : IValueConverter
{
    public static readonly string[] NiceMessages = ["✔", "⚠", "⛔", "⌛"];

    public static readonly Dictionary<string, LogMessageType> NiceMessagesRev = new()
    {
        { "✔", LogMessageType.ok},
        { "⚠", LogMessageType.warning},
        { "⛔", LogMessageType.error},
        { "⌛", LogMessageType.inProgress},
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogMessageType logMessageType)
        {
            return NiceMessages[(int)logMessageType];
        }
        else if (value is DateTime dateTime)
        {
            return dateTime.ToString("HH:mm:ss ddd");
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(LogMessageType))
        {
            return NiceMessagesRev[value.ToString()];
        }
        else
        {
            if (DateTime.TryParse(value.ToString(), out var dt))
            {
                return dt;
            }

            return DateTime.UnixEpoch;
        }
    }
}