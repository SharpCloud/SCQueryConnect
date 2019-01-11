using SCQueryConnect.Common;
using SCQueryConnect.Interfaces;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SCQueryConnect.Helpers
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection GetDb(QueryData data)
        {
            switch (data.ConnectionType)
            {
                case DatabaseType.SQL:
                    return new SqlConnection(data.FormattedConnectionString);

                case DatabaseType.ODBC:
                    return new OdbcConnection(data.FormattedConnectionString);

                case DatabaseType.SharpCloud:
                    var excelConnectionString = Regex.Replace(
                        data.ConnectionsString,
                        "Source Story=.+?;",
                        string.Empty,
                        RegexOptions.IgnoreCase);

                    return new OleDbConnection(excelConnectionString);

                default:
                    return new OleDbConnection(data.FormattedConnectionString);
            }
        }
    }
}
