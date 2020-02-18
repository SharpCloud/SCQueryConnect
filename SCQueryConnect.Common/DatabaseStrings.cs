using System.Collections.Generic;
using System.Linq;

namespace SCQueryConnect.Common
{
    public static class DatabaseStrings
    {
        public const string MsAdeAccess = "MS_ADE_ACCESS";
        public const string MsAdeExcel = "MS_ADE_EXCEL";
        public const string Sql = "SQL";
        public const string Odbc = "ODBC";
        public const string OleDb = "OLEDB";
        public const string MsAdeSharePointList = "MS_ADE_SHAREPOINT_LIST";
        public const string MsAdeSharpCloudExcel = "MS_ADE_SHARPCLOUD_EXCEL";

        public const string ExcelFileKey = "Excel File";
        public const string DataSourceKey = "Data Source";

        public static Dictionary<DatabaseType, string> TypeStringMapping
            = new Dictionary<DatabaseType, string>
            {
                [DatabaseType.MsAdeAccess] = MsAdeAccess,
                [DatabaseType.MsAdeExcel] = MsAdeExcel,
                [DatabaseType.Sql] = Sql,
                [DatabaseType.Odbc] = Odbc,
                [DatabaseType.MsAdeSharePointList] = MsAdeSharePointList,
                [DatabaseType.MsAdeSharpCloudExcel] = MsAdeSharpCloudExcel
            };

        public static Dictionary<string, DatabaseType> StringTypeMapping
            = TypeStringMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
