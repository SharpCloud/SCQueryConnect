using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace SCSQLBatch
{
    class Program
    {
        static ILog logger = new ConsoleLogger();

        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static async Task Run()
        {
            var builder = CreateContainerBuilder();
            using (var iocContainer = builder.Build())
            {
                var userid = ConfigurationManager.AppSettings["userid"];
                var password = ConfigurationManager.AppSettings["password"];
                var password64 = ConfigurationManager.AppSettings["password64"];
                var passwordDpapi = ConfigurationManager.AppSettings["passwordDpapi"];
                var url = ConfigurationManager.AppSettings["url"];
                var storyid = ConfigurationManager.AppSettings["storyid"];
                var connectionString = ConfigurationManager.AppSettings["connectionString"];
                var queryString = ConfigurationManager.AppSettings["queryString"];
                var queryStringRels = ConfigurationManager.AppSettings["queryStringRels"];
                bool.TryParse(ConfigurationManager.AppSettings["unpublishItems"], out var unpubItems);
                var proxy = ConfigurationManager.AppSettings["proxy"];
                bool.TryParse(ConfigurationManager.AppSettings["proxyAnonymous"], out var proxyAnonymous);
                var proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
                var proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
                var proxyPassword64 = ConfigurationManager.AppSettings["proxyPassword64"];
                var proxyPasswordDpapi = ConfigurationManager.AppSettings["proxyPasswordDpapi"];

                var encryptionHelper = iocContainer.Resolve<IEncryptionHelper>();

                var qcHelper = iocContainer.Resolve<IQueryConnectHelper>(
                    new NamedParameter("log", logger));

                // basic checks
                if (string.IsNullOrEmpty(userid) || userid == "USERID")
                {
                    await logger.Log("Error: No username provided.");
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    // set the password from encoded password

                    var hasPassword64 = !string.IsNullOrWhiteSpace(password64);

                    if (hasPassword64)
                    {
                        password = Encoding.Default.GetString(
                            Convert.FromBase64String(password64));
                    }
                    else
                    {
                        password = encryptionHelper.TextEncoding.GetString(
                            encryptionHelper.Decrypt(passwordDpapi));
                    }

                    if (string.IsNullOrEmpty(passwordDpapi))
                    {
                        await logger.Log("Error: No password provided.");
                        return;
                    }
                }

                if (string.IsNullOrEmpty(url))
                {
                    await logger.Log("Error: No URL provided.");
                    return;
                }

                if (string.IsNullOrEmpty(storyid) || userid == "00000000-0000-0000-0000-000000000000")
                {
                    await logger.Log("Error: No storyID provided.");
                    return;
                }

                if (string.IsNullOrEmpty(connectionString) || connectionString == "CONNECTIONSTRING")
                {
                    await logger.Log("Error: No connection string provided.");
                    return;
                }

                if (string.IsNullOrEmpty(queryString) || userid == "QUERYSTRING")
                {
                    await logger.Log("Error: No database query provided.");
                    return;
                }

                if (!string.IsNullOrEmpty(proxy) && !proxyAnonymous)
                {
                    if (string.IsNullOrEmpty(proxyPassword))
                    {
                        var hasProxyPassword64 = !string.IsNullOrWhiteSpace(proxyPassword64);

                        if (hasProxyPassword64)
                        {
                            proxyPassword = Encoding.Default.GetString(
                                Convert.FromBase64String(proxyPassword64));
                        }
                        else
                        {
                            proxyPassword = encryptionHelper.TextEncoding.GetString(
                                encryptionHelper.Decrypt(proxyPasswordDpapi));
                        }
                    }

                    if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
                    {
                        await logger.Log("Error: No proxy username or password provided.");
                    }
                }
                // do the work

                var dbType = GetDbType();

                var config = new SharpCloudConfiguration
                {
                    Username = userid,
                    Password = password,
                    Url = url,
                    ProxyUrl = proxy,
                    UseDefaultProxyCredentials = proxyAnonymous,
                    ProxyUserName = proxyUsername,
                    ProxyPassword = proxyPassword
                };

                var settings = new UpdateSettings
                {
                    TargetStoryId = storyid,
                    QueryString = queryString,
                    QueryStringRels = queryStringRels,
                    ConnectionString = connectionString,
                    DBType = dbType,
                    MaxRowCount = 1000,
                    UnpublishItems = unpubItems
                };

                await qcHelper.UpdateSharpCloud(config, settings);
            }
        }

        private static bool TypeIsNumeric(Type type)
        {
            return type == typeof(double) || type == typeof(int) || type == typeof(float) || type == typeof(decimal) ||
                type == typeof(short) || type == typeof(long) || type == typeof(byte) || type == typeof(SByte) ||
                type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64);
        }

        private static DatabaseType GetDbType()
        {
            var dbType = ConfigurationManager.AppSettings["dbType"];

            switch (dbType)
            {
                case DatabaseStrings.Sql:
                    return DatabaseType.SQL;
                case DatabaseStrings.Odbc:
                    return DatabaseType.ODBC;
                default:
                case DatabaseStrings.Oledb:
                    return DatabaseType.Excel;
                case DatabaseStrings.SharpCloudExcel:
                    return DatabaseType.SharpCloudExcel;
            }
        }

        private static ContainerBuilder CreateContainerBuilder()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ArchitectureDetector>().As<IArchitectureDetector>();
            builder.RegisterType<ConnectionStringHelper>().As<IConnectionStringHelper>();
            builder.RegisterType<DataChecker>().As<IDataChecker>();
            builder.RegisterType<DpapiHelper>().As<IEncryptionHelper>();
            builder.RegisterType<DbConnectionFactory>().As<IDbConnectionFactory>();
            builder.RegisterType<ExcelWriter>().As<IExcelWriter>();
            builder.RegisterType<RelationshipsDataChecker>().As<IRelationshipsDataChecker>();
            builder.RegisterType<SharpCloudApiFactory>().As<ISharpCloudApiFactory>();
            builder.RegisterType<ConsoleLogger>().As<ILog>();
            builder.RegisterType<QueryConnectHelper>().As<IQueryConnectHelper>();

            return builder;
        }
    }
}
