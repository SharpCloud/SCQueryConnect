using System;
using System.Globalization;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class EnumMatchToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            var valueString = value.ToString();
            var paramString = parameter.ToString();
            
            var isMatch = valueString.Equals(paramString,
                StringComparison.InvariantCultureIgnoreCase);

            return isMatch;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return null;
            }

            var paramString = parameter.ToString();
            object toReturn = null;

            if ((bool) value)
            {
                toReturn = Enum.Parse(targetType, paramString);
            }

            return toReturn;
        }
    }
}
