using System;
using System.Globalization;
using WApplication = System.Windows.Application;
using WDependencyProperty = System.Windows.DependencyProperty;
using WImageSource = System.Windows.Media.ImageSource;
using WBinding = System.Windows.Data.Binding;
using WData = System.Windows.Data;

namespace ESCenter.Converters
{
    public class SeriesKeyToIconConverter : WData.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value as string ?? string.Empty;
            var iconResourceKey = key switch
            {
                "ongoing" => "CharOngoing",
                "closed" => "CharFinished",
                "Major" => "CharMajor",
                "Critical" => "CharCritical",
                "Overdue" => "CharOverdue",
                _ => "Charts"
            };

            var image = WApplication.Current.TryFindResource(iconResourceKey);
            return image as WImageSource ?? WDependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => WBinding.DoNothing;
    }
}
