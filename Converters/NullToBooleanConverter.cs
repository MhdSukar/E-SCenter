using System.Globalization;
using System.Windows.Data;

namespace ESCenter.Converters
{
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            var isNull = value == null;
            var invert = string.Equals(parameter?.ToString(), "Invert=true", System.StringComparison.OrdinalIgnoreCase)
                         || string.Equals(parameter?.ToString(), "true", System.StringComparison.OrdinalIgnoreCase);
            return invert ? !isNull : isNull;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
