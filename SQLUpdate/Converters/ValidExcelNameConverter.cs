using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class ValidExcelNameConverter : IValueConverter
    {
        private const string AccessKey = "Access";
        private const string ExcelKey = "Excel";

        private readonly Dictionary<string, HashSet<string>> _validFileExtensions
            = new Dictionary<string, HashSet<string>>
            {
                [ExcelKey] = new HashSet<string>
                {
                    ".xls",
                    ".xlsb",
                    ".xlsm",
                    ".xlsx"
                },
                [AccessKey] = new HashSet<string>
                {
                    ".accdb"
                }
            };

        private readonly Dictionary<string, string> _defaultExtensions
            = new Dictionary<string, string>
            {
                [ExcelKey] = ".xlsx",
                [AccessKey] = ".accdb"
            };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetValidFilename(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetValidFilename(value, parameter);
        }

        private string GetValidFilename(object value, object parameter)
        {
            if (value is string filename && parameter is string key)
            {
                var ext = Path.GetExtension(filename);
                
                var isValid =
                    _validFileExtensions.ContainsKey(key) &&
                    _validFileExtensions[key].Contains(ext);

                var suffix = isValid
                    ? string.Empty
                    : _defaultExtensions[key];

                var toReturn = $"{filename}{suffix}";
                return toReturn;
            }

            return string.Empty;
        }
    }
}
