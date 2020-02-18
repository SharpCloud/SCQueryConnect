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
        private const string Delimiter = ";";

        private readonly ICDataLicenceService _cDataLicenceService;

        public DbConnectionFactory(ICDataLicenceService cDataLicenceService)
        {
            _cDataLicenceService = cDataLicenceService;
        }

        public IDbConnection GetDb(string connectionString, DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.Excel:
                    return new ExcelConnection(connectionString)
                    {
                        RuntimeLicense = _cDataLicenceService.GetLicence(dbType)
                    };

                case DatabaseType.Access:
                    return new AccessConnection(connectionString)
                    {
                        RuntimeLicense = _cDataLicenceService.GetLicence(dbType)
                    };

                case DatabaseType.SharePointList:
                    return new SharePointConnection(connectionString)
                    {
                        RuntimeLicense = _cDataLicenceService.GetLicence(dbType)
                    };

                case DatabaseType.Sql:
                    return new SqlConnection(connectionString);

                case DatabaseType.Odbc:
                    return new OdbcConnection(connectionString);

                case DatabaseType.SharpCloudExcel:
                {
                    var variables = new List<string>
                    {
                        "SourceId",
                        "SourceUserName",
                        "SourcePassword",
                        "SourceServer"
                    };

                    var excelConnectionString = string.Join(
                        Delimiter,
                        connectionString
                            .Split(Delimiter[0])
                            .Where(kvp => !variables.Contains(kvp.Split('=')[0])));

                    return new ExcelConnection(excelConnectionString);
                }

                case DatabaseType.MsAdeSharpCloudExcel:
                {
                    var variables = new List<string>
                    {
                        "SourceId",
                        "SourceUserName",
                        "SourcePassword",
                        "SourceServer"
                    };

                    var excelConnectionString = string.Join(
                        Delimiter,
                        connectionString
                            .Split(Delimiter[0])
                            .Where(kvp => !variables.Contains(kvp.Split('=')[0])));

                    return new OleDbConnection(excelConnectionString);
                }

                default:
                    return new OleDbConnection(connectionString);
            }
        }
    }
}
