using System;
using System.Globalization;
using ESCenter.Core;
using WData = System.Windows.Data;
using WDependencyProperty = System.Windows.DependencyProperty;
using WMedia = System.Windows.Media;

namespace ESCenter.Converters
{
    public class StatusLevelToBrushConverter : WData.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not StatusLevel level)
            {
                return WMedia.Brushes.DeepSkyBlue;
            }

            return level switch
            {
                StatusLevel.Success => WMedia.Brushes.LimeGreen,
                StatusLevel.Warning => WMedia.Brushes.Orange,
                StatusLevel.Error => WMedia.Brushes.IndianRed,
                _ => WMedia.Brushes.DeepSkyBlue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => WDependencyProperty.UnsetValue;
    }
}
