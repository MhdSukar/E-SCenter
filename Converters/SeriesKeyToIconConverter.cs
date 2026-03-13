using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ESCenter.Converters
{
    public class SeriesKeyToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value as string ?? string.Empty;
            var iconResourceKey = key switch
            {
                "Open" => "Tickets",
                "Finished" => "TickClosed",
                "Critical" => "Priority",
                "Overdue" => "ReadyToPick",
                _ => "Charts"
            };

            var image = Application.Current.TryFindResource(iconResourceKey);
            return image as ImageSource ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
