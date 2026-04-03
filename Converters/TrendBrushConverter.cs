using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ESCenter.Converters;

public class TrendBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || !decimal.TryParse(value.ToString(), out var trend)) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E"));
        if (trend > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30C878"));
        if (trend < 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3737"));
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
