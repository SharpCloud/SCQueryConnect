using SCQueryConnect.Common.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Data.CData.Access;
using System.Data.CData.Excel;
using System.Data.CData.SharePoint;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;

namespace SCQueryConnect.Common.Helpers
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private const string _delimiter = ";";

        public IDbConnection GetDb(string connectionString, DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.Excel:
                    return new ExcelConnection(connectionString);

                case DatabaseType.Access:
                    return new AccessConnection(connectionString);

                case DatabaseType.SharepointList:
                    return new SharePointConnection(connectionString);

                case DatabaseType.SQL:
                    return new SqlConnection(connectionString);

                case DatabaseType.ODBC:
                    return new OdbcConnection(connectionString);

                case DatabaseType.SharpCloudExcel:
                    var variables = new List<string>()
                    {
                        "SourceId",
                        "SourceUserName",
                        "SourcePassword",
                        "SourceServer"
                    };

                    var excelConnectionString = string.Join(
                        _delimiter,
                        connectionString
                            .Split(_delimiter[0])
                            .Where(kvp => !variables.Contains(kvp.Split('=')[0])));

                    return new ExcelConnection(excelConnectionString);

                default:
                    return new OleDbConnection(connectionString);
            }
        }
    }
}
