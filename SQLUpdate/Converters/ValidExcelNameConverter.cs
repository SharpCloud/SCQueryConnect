using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class ValidExcelNameConverter : IValueConverter
    {
        private readonly HashSet<string> _validFileExtensions
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".xls",
            ".xlsb",
            ".xlsm",
            ".xlsx"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetValidFilename(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetValidFilename(value);
        }

        private string GetValidFilename(object value)
        {
            if (value is string filename)
            {
                var ext = Path.GetExtension(filename);
                var isValid = _validFileExtensions.Contains(ext);

                var suffix = isValid
                    ? string.Empty
                    : ".xlsx";

                var toReturn = $"{filename}{suffix}";
                return toReturn;
            }

            return string.Empty;
        }
    }
}
