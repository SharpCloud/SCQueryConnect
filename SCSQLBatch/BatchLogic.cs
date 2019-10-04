using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SCSQLBatch
{
    public class BatchLogic
    {
        private readonly IConfigurationReader _configurationReader;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ILog _logger;
        private readonly IQueryConnectHelper _qcHelper;

        public BatchLogic(
            IConfigurationReader configurationReader,
            IEncryptionHelper encryptionHelper,
            ILog logger,
            IQueryConnectHelper qcHelper)
        {
            _configurationReader = configurationReader;
            _encryptionHelper = encryptionHelper;
            _logger = logger;
            _qcHelper = qcHelper;
        }

        public async Task Run()
        {
            var userid = _configurationReader.Get("userid");
            var password = _configurationReader.Get("password");
            var password64 = _configurationReader.Get("password64");
            var passwordDpapi = _configurationReader.Get("passwordDpapi");
            var url = _configurationReader.Get("url");
            var storyid = _configurationReader.Get("storyid");
            var connectionString = _configurationReader.Get("connectionString");
            var queryString = _configurationReader.Get("queryString");
            var queryStringRels = _configurationReader.Get("queryStringRels");
            bool.TryParse(_configurationReader.Get("unpublishItems"), out var unpubItems);
            var proxy = _configurationReader.Get("proxy");
            bool.TryParse(_configurationReader.Get("proxyAnonymous"), out var proxyAnonymous);
            var proxyUsername = _configurationReader.Get("proxyUsername");
            var proxyPassword = _configurationReader.Get("proxyPassword");
            var proxyPassword64 = _configurationReader.Get("proxyPassword64");
            var proxyPasswordDpapi = _configurationReader.Get("proxyPasswordDpapi");

            // basic checks
            if (string.IsNullOrEmpty(userid) || userid == "USERID")
            {
                await _logger.Log("Error: No username provided.");
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
                    password = _encryptionHelper.TextEncoding.GetString(
                        _encryptionHelper.Decrypt(passwordDpapi));
                }

                if (string.IsNullOrEmpty(password))
                {
                    await _logger.Log("Error: No password provided.");
                    return;
                }
            }

            if (string.IsNullOrEmpty(url))
            {
                await _logger.Log("Error: No URL provided.");
                return;
            }

            if (string.IsNullOrEmpty(storyid) || userid == "00000000-0000-0000-0000-000000000000")
            {
                await _logger.Log("Error: No storyID provided.");
                return;
            }

            if (string.IsNullOrEmpty(connectionString) || connectionString == "CONNECTIONSTRING")
            {
                await _logger.Log("Error: No connection string provided.");
                return;
            }

            if (string.IsNullOrEmpty(queryString) || userid == "QUERYSTRING")
            {
                await _logger.Log("Error: No database query provided.");
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
                        proxyPassword = _encryptionHelper.TextEncoding.GetString(
                            _encryptionHelper.Decrypt(proxyPasswordDpapi));
                    }
                }

                if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
                {
                    await _logger.Log("Error: No proxy username or password provided.");
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

            await _qcHelper.UpdateSharpCloud(config, settings);
        }

        private DatabaseType GetDbType()
        {
            var dbType = _configurationReader.Get("dbType");

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
    }
}
