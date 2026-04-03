using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ESCenter.Converters;

public class MarginBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || !decimal.TryParse(value.ToString(), out var margin)) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3737"));
        if (margin > 50m) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30C878"));
        if (margin >= 20m) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCA000"));
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3737"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
