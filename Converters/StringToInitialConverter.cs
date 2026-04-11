using System;
using System.Globalization;
using System.Windows.Data;

namespace ESCenter.Converters
{
    public class StringToInitialConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
                return name.Trim()[0].ToString().ToUpper();

            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
