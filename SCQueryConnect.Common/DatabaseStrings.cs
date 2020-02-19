using System.Collections.Generic;
using System.Linq;

namespace SCQueryConnect.Common
{
    public static class DatabaseStrings
    {
        public const string Access = "ACCESS";
        public const string Excel = "EXCEL";
        public const string MsAdeAccess = "MS_ADE_ACCESS";
        public const string MsAdeExcel = "MS_ADE_EXCEL";
        public const string MsAdeSharePointList = "MS_ADE_SHAREPOINT_LIST";
        public const string MsAdeSharpCloudExcel = "MS_ADE_SHARPCLOUD_EXCEL";
        public const string Sql = "SQL";
        public const string Odbc = "ODBC";
        public const string OleDb = "OLEDB";
        public const string SharePointList = "SHAREPOINT_LIST";
        public const string SharpCloudExcel = "SHARPCLOUD_EXCEL";

        public const string ExcelFileKey = "Excel File";
        public const string DataSourceKey = "Data Source";

        public static Dictionary<DatabaseType, string> TypeStringMapping
            = new Dictionary<DatabaseType, string>
            {
                [DatabaseType.Access] = Access,
                [DatabaseType.Excel] = Excel,
                [DatabaseType.MsAdeAccess] = MsAdeAccess,
                [DatabaseType.MsAdeExcel] = MsAdeExcel,
                [DatabaseType.MsAdeSharePointList] = MsAdeSharePointList,
                [DatabaseType.MsAdeSharpCloudExcel] = MsAdeSharpCloudExcel,
                [DatabaseType.Sql] = Sql,
                [DatabaseType.Odbc] = Odbc,
                [DatabaseType.SharePointList] = SharePointList,
                [DatabaseType.SharpCloudExcel] = SharpCloudExcel
            };

        public static Dictionary<string, DatabaseType> StringTypeMapping
            = TypeStringMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
