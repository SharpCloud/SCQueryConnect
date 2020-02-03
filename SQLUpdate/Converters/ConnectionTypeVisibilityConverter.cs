using SCQueryConnect.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public abstract class ConnectionTypeVisibilityConverter : IValueConverter
    {
        protected abstract Visibility MatchVisibility { get; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = Visibility.Collapsed;

            if (value is DatabaseType valueType &&
                parameter is string paramString)
            {
                var tokenSuccess = true;
                var tokens = paramString.Split(',').Select(s => s.Trim()).ToArray();
                var dbTypes = new List<DatabaseType>();

                for (var i = 0; tokenSuccess && i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    tokenSuccess = Enum.TryParse(token, out DatabaseType dbType);

                    if (tokenSuccess)
                    {
                        dbTypes.Add(dbType);
                    }
                }

                var match = dbTypes.Contains(valueType);
                
                if (match)
                {
                    visibility = MatchVisibility;
                }
                else
                {
                    visibility = MatchVisibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
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
