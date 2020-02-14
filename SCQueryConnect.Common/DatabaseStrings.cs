using System.Collections.Generic;
using System.Linq;

namespace SCQueryConnect.Common
{
    public static class DatabaseStrings
    {
        public const string Access = "ACCESS";
        public const string Excel = "EXCEL";
        public const string Sql = "SQL";
        public const string Odbc = "ODBC";
        public const string Oledb = "OLEDB";
        public const string SharpCloudExcel = "SHARPCLOUD_EXCEL";

        public const string DataSourceKey = "Data Source";

        public static Dictionary<DatabaseType, string> TypeStringMapping
            = new Dictionary<DatabaseType, string>
            {
                [DatabaseType.Access] = Access,
                [DatabaseType.Excel] = Excel,
                [DatabaseType.SQL] = Sql,
                [DatabaseType.ODBC] = Odbc,
                [DatabaseType.SharpCloudExcel] = SharpCloudExcel
            };

        public static Dictionary<string, DatabaseType> StringTypeMapping
            = TypeStringMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
