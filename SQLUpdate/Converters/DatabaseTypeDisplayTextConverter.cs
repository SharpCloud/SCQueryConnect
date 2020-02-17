using SCQueryConnect.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SCQueryConnect.Converters
{
    public class DatabaseTypeDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dbType = (DatabaseType) value;

            switch (dbType)
            {
                case DatabaseType.Sql:
                    return "SQL";
                case DatabaseType.Odbc:
                    return "ODBC";
                case DatabaseType.Ado:
                    return "ADO";
                case DatabaseType.MsAdeExcel:
                    return "Excel (ADE)";
                case DatabaseType.MsAdeAccess:
                    return "Access (ADE)";
                case DatabaseType.MsAdeSharePointList:
                    return "SharePoint List (ADE)";
                case DatabaseType.MsAdeSharpCloudExcel:
                    return "SharpCloud (Excel/ADE)";
                case DatabaseType.Folder:
                    return "Folder";
                case DatabaseType.Excel:
                    return "Excel";
                case DatabaseType.Access:
                    return "Access";
                case DatabaseType.SharePointList:
                    return "SharePoint List";
                case DatabaseType.SharpCloudExcel:
                    return "SharpCloud (Excel)";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
