using System;
using System.Globalization;
using ESCenter.Core;
using WData = System.Windows.Data;
using WDependencyProperty = System.Windows.DependencyProperty;

namespace ESCenter.Converters
{
    public class StatusLevelToIconConverter : WData.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not StatusLevel level)
            {
                return "\uE946";
            }

            return level switch
            {
                StatusLevel.Success => "\uE73E",
                StatusLevel.Warning => "\uE7BA",
                StatusLevel.Error => "\uEA39",
                _ => "\uE946"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => WDependencyProperty.UnsetValue;
    }
}
