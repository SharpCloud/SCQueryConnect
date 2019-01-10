using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace SCSQLBatch
{
    class Program
    {
        static bool unpublishItems = false;
        static ILog logger = new ConsoleLogger();

        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static async Task Run()
        {
            var userid = ConfigurationManager.AppSettings["userid"];
            var password = ConfigurationManager.AppSettings["password"];
            var password64 = ConfigurationManager.AppSettings["password64"];
            var url = ConfigurationManager.AppSettings["url"];
            var storyid = ConfigurationManager.AppSettings["storyid"];
            var connectionString = ConfigurationManager.AppSettings["connectionString"];
            var queryString = ConfigurationManager.AppSettings["queryString"];
            var queryStringRels = ConfigurationManager.AppSettings["queryStringRels"];
            bool unpubItems;
            if (bool.TryParse(ConfigurationManager.AppSettings["unpublishItems"], out unpubItems))
                unpublishItems = unpubItems;
            var proxy = ConfigurationManager.AppSettings["proxy"];
            bool proxyAnonymous = true;
            bool.TryParse(ConfigurationManager.AppSettings["proxyAnonymous"], out proxyAnonymous);
            var proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
            var proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
            var proxyPassword64 = ConfigurationManager.AppSettings["proxyPassword64"];

            var qcHelper = new QueryConnectHelper(
                new DataChecker(),
                logger,
                new RelationshipsDataChecker());

            // basic checks
            if (string.IsNullOrEmpty(userid) || userid == "USERID")
            {
                await logger.Log("Error: No username provided.");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                // set the password from the encoded password
                password = Encoding.Default.GetString(Convert.FromBase64String(password64));
                if (string.IsNullOrEmpty(password64))
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
                if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
                {
                    await logger.Log("Error: No proxy username or password provided.");
                }
                if (string.IsNullOrEmpty(proxyPassword))
                {
                    proxyPassword = Encoding.Default.GetString(Convert.FromBase64String(proxyPassword64));
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
                case DatabaseTypeStrings.Sql:
                    return DatabaseType.SQL;
                case DatabaseTypeStrings.Odbc:
                    return DatabaseType.ODBC;
                default:
                case DatabaseTypeStrings.Oledb:
                    return DatabaseType.Excel;
                case DatabaseTypeStrings.SharpCloud:
                    return DatabaseType.SharpCloud;
            }
        }
    }
}
