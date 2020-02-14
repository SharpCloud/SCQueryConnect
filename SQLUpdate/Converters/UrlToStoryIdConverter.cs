using System;
using System.Globalization;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class UrlToStoryIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var newValue = (string) value;
            if (newValue != null && newValue.Contains("#/story"))
            {
                var mid = newValue.Substring(newValue.IndexOf("#/story", StringComparison.Ordinal) + 8);
                if (mid.Length >= 36)
                {
                    newValue = mid.Substring(0, 36);
                }
            }

            return newValue;
        }
    }
}
