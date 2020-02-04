using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = 
                value is string message &&
                !string.IsNullOrWhiteSpace(message)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
