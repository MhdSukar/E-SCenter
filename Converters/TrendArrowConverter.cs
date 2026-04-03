using System;
using System.Globalization;
using System.Windows.Data;

namespace ESCenter.Converters;

public class TrendArrowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || !decimal.TryParse(value.ToString(), out var trend)) return "•";
        return trend > 0 ? "▲" : trend < 0 ? "▼" : "•";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
