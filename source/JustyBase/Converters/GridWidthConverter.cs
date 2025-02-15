using System;
using System.Globalization;

namespace JustyBase.Converters;

public sealed class GridWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringObject)
        {
            if (stringObject == "*")
            {
                return GridLength.Star;
            }
            else
            {
                return GridLength.Parse(stringObject);
            }
        }

        return GridLength.Star;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value.ToString();
    }
}


//public class SpecialConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        if ((value as DataGridCell)?.Content is Avalonia.Controls.TextBlock tb)
//        {
//            if (tb.Text == "2005 01 01")
//            {
//                return new SolidColorBrush(Colors.LightPink, 0.8);
//            }
//        }
//        return null;
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}

//public class NullStyleConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        if (value is null)
//        {
//            //return Avalonia.Media.Brushes.Gray;
//            return Avalonia.Media.Brushes.Gray;
//        }
//        //return Avalonia.Media.Brushes.Red;
//        return Random.Shared.NextDouble() > 0.5 ? Avalonia.Media.Brushes.Green : Avalonia.Media.Brushes.Yellow;

//        //if (App.Current.ActualThemeVariant.Key.ToString() == "Dark")
//        //{
//        //    return Avalonia.Media.Brushes.White;
//        //}
//        //return Avalonia.Media.Brushes.Black;
//        //(App.Current.Resources.Owner as IResourceHost).FindResource("TextControlBackground");

//        //return Avalonia.Media.Brushes.Black;
//        //return Avalonia.Media.Brushes.Transparent;
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}