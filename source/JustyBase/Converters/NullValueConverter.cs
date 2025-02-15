using System;
using System.Globalization;

namespace JustyBase.Converters;

//result grid
public sealed class NullValueConverter : IValueConverter
{
    public const string datetimeFormat = "yyyy-MM-dd HH:mm:ss";
    public string NumericFormat = "N8";
    public string NumericIntFormat = "N0";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(bool?))
        {
            return value;
        }
        else if (value is null)
        {
            //return "   #NULL";
            //return " ■◼NULL";
            //return " ■ NULL";
            return "● NULL";
        }
        else if (value is DateTime dateTime)
        {
            return dateTime.ToString(datetimeFormat);
        }
        else if (value is float floatVal)
        {
            return floatVal.ToString(NumericFormat);
        }
        else if (value is double doubleVal)
        {
            return doubleVal.ToString(NumericFormat);
        }
        else if (value is decimal decimalVal)
        {
            return decimalVal.ToString(NumericFormat);
        }
        else if (value is short shortVal)
        {
            return shortVal.ToString(NumericIntFormat);
        }
        else if (value is int intVal)
        {
            return intVal.ToString(NumericIntFormat);
        }
        else if (value is long longVal)
        {
            return longVal.ToString(NumericIntFormat);
        }
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
