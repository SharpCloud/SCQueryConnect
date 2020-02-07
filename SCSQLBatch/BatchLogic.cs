using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
            var userid = _configurationReader.Get(Constants.BatchUserIdKey);
            var password = _configurationReader.Get(Constants.BatchPasswordKey);
            var password64 = _configurationReader.Get(Constants.BatchPassword64Key);
            var passwordDpapi = _configurationReader.Get(Constants.BatchPasswordDpapiKey);
            var passwordDpapiEntropy = _configurationReader.Get(Constants.BatchPasswordDpapiEntropyKey);
            var url = _configurationReader.Get(Constants.BatchUrlKey);
            var storyId = _configurationReader.Get(Constants.BatchStoryIdKey);
            var connectionString = _configurationReader.Get(Constants.BatchConnectionStringKey);
            var queryString = _configurationReader.Get(Constants.BatchQueryStringKey);
            var queryStringRels = _configurationReader.Get(Constants.BatchQueryStringRelsKey);
            var queryStringPanelsKey = _configurationReader.Get(Constants.BatchQueryStringPanelsKey);
            var queryStringResourceUrlsKey = _configurationReader.Get(Constants.BatchQueryStringResourceUrlsKey);
            bool.TryParse(_configurationReader.Get(Constants.BatchBuildRelationshipsKey), out var buildRelationships);
            bool.TryParse(_configurationReader.Get(Constants.BatchUnpublishItemsKey), out var unpublishItems);
            var proxy = _configurationReader.Get(Constants.BatchProxyKey);
            bool.TryParse(_configurationReader.Get(Constants.BatchProxyAnonymousKey), out var proxyAnonymous);
            var proxyUsername = _configurationReader.Get(Constants.BatchProxyUsernameKey);
            var proxyPassword = _configurationReader.Get(Constants.BatchProxyPasswordKey);
            var proxyPassword64 = _configurationReader.Get(Constants.BatchProxyPassword64Key);
            var proxyPasswordDpapi = _configurationReader.Get(Constants.BatchProxyPasswordDpapiKey);
            var proxyPasswordDpapiEntropy = _configurationReader.Get(Constants.BatchProxyPasswordDpapiEntropyKey);

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
                        _encryptionHelper.Decrypt(
                            passwordDpapi,
                            passwordDpapiEntropy,
                            DataProtectionScope.LocalMachine));
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

            if (string.IsNullOrEmpty(storyId) || userid == "00000000-0000-0000-0000-000000000000")
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
                            _encryptionHelper.Decrypt(
                                proxyPasswordDpapi,
                                proxyPasswordDpapiEntropy,
                                DataProtectionScope.LocalMachine));
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
                BuildRelationships = buildRelationships,
                TargetStoryId = storyId,
                QueryString = queryString,
                QueryStringRels = queryStringRels,
                QueryStringPanels = queryStringPanelsKey,
                QueryStringResourceUrls = queryStringResourceUrlsKey,
                ConnectionString = connectionString,
                DBType = dbType,
                MaxRowCount = 1000,
                UnpublishItems = unpublishItems
            };

            await _qcHelper.UpdateSharpCloud(config, settings, CancellationToken.None);
        }

        private DatabaseType GetDbType()
        {
            var dbType = _configurationReader.Get(Constants.BatchDBTypeKey);

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
