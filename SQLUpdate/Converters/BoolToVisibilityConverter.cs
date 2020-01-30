using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = Visibility.Collapsed;

            if (value is bool boolValue &&
                parameter is string paramString)
            {
                var success = bool.TryParse(paramString, out var boolParam);

                if (success && boolValue == boolParam)
                {
                    visibility = Visibility.Visible;
                }
            }

            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
