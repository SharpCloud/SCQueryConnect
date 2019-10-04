using System.Text;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCSQLBatch;

namespace SCSqlBatch.Tests
{
    [TestFixture]
    public class BatchLogicTests
    {
        [Test]
        public async Task PreferPasswordIfPresent()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == "BatchPassword" &&
                r.Get("password64") == "QmF0Y2hQYXNzd29yZDY0" && // 'BatchPassword64'
                r.Get("passwordDpapi") == "BatchPasswordDpapi" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == "BatchProxyPassword" &&
                r.Get("proxyPassword64") == "BatchProxyPassword64" &&
                r.Get("proxyPasswordDpapi") == "BatchProxyPasswordDpapi" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration) configMock.Invocations[0].Arguments[0];
            Assert.AreEqual("BatchPassword", config.Password);
        }

        [Test]
        public async Task PreferPassword64IfPresentAndPasswordUnavailable()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == string.Empty &&
                r.Get("password64") == "QmF0Y2hQYXNzd29yZDY0" && // 'BatchPassword64'
                r.Get("passwordDpapi") == "BatchPasswordDpapi" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == "BatchProxyPassword" &&
                r.Get("proxyPassword64") == "BatchProxyPassword64" &&
                r.Get("proxyPasswordDpapi") == "BatchProxyPasswordDpapi" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual("BatchPassword64", config.Password);
        }

        [Test]
        public async Task Password64OnlyIsPermissible()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == string.Empty &&
                r.Get("password64") == "QmF0Y2hQYXNzd29yZDY0" && // 'BatchPassword64'
                r.Get("passwordDpapi") == string.Empty &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == "BatchProxyPassword" &&
                r.Get("proxyPassword64") == "BatchProxyPassword64" &&
                r.Get("proxyPasswordDpapi") == "BatchProxyPasswordDpapi" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual("BatchPassword64", config.Password);
        }

        [Test]
        public async Task PreferPasswordDpapiIfPresentAndPasswordAndPassword64Unavailable()
        {
            // Arrange

            const string passwordDpapi = "BatchPasswordDpapi";

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == string.Empty &&
                r.Get("password64") == string.Empty &&
                r.Get("passwordDpapi") == passwordDpapi &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == "BatchProxyPassword" &&
                r.Get("proxyPassword64") == "BatchProxyPassword64" &&
                r.Get("proxyPasswordDpapi") == "BatchProxyPasswordDpapi" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>(h =>
                h.TextEncoding == Encoding.UTF8 &&
                h.Decrypt(It.IsAny<string>()) == Encoding.UTF8.GetBytes(passwordDpapi));

            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual(passwordDpapi, config.Password);
        }

        [Test]
        public async Task PreferProxyPasswordIfPresent()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == "BatchPassword" &&
                r.Get("password64") == "BatchPassword64" &&
                r.Get("passwordDpapi") == "BatchPasswordDpapi" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == "BatchProxyPassword" &&
                r.Get("proxyPassword64") == "QmF0Y2hQcm94eVBhc3N3b3JkNjQ=" && // 'BatchProxyPassword64'
                r.Get("proxyPasswordDpapi") == "BatchProxyPasswordDpapi" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual("BatchProxyPassword", config.ProxyPassword);
        }

        [Test]
        public async Task PreferProxyPassword64IfPresentAndProxyPasswordUnavailable()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == "BatchPassword" &&
                r.Get("password64") == "BatchPassword64" &&
                r.Get("passwordDpapi") == "BatchPasswordDpapi" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == string.Empty &&
                r.Get("proxyPassword64") == "QmF0Y2hQcm94eVBhc3N3b3JkNjQ=" && // 'BatchProxyPassword64'
                r.Get("proxyPasswordDpapi") == "BatchProxyPasswordDpapi" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual("BatchProxyPassword64", config.ProxyPassword);
        }

        [Test]
        public async Task ProxyPassword64OnlyIsPermissible()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == "BatchPassword" &&
                r.Get("password64") == "BatchPassword64" &&
                r.Get("passwordDpapi") == "BatchPasswordDpapi" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == string.Empty &&
                r.Get("proxyPassword64") == "QmF0Y2hQcm94eVBhc3N3b3JkNjQ=" && // 'BatchProxyPassword64'
                r.Get("proxyPasswordDpapi") == string.Empty &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual("BatchProxyPassword64", config.ProxyPassword);
        }

        [Test]
        public async Task PreferProxyPasswordDpapiIfPresentAndProxyPasswordAndProxyPassword64Unavailable()
        {
            // Arrange

            const string proxyPasswordDpapi = "BatchProxyPasswordDpapi";

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == "BatchPassword" &&
                r.Get("password64") == "BatchPassword64" &&
                r.Get("passwordDpapi") == "BatchPasswordDpapi" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "BatchUnpublishItems" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "BatchProxyAnonymous" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == string.Empty &&
                r.Get("proxyPassword64") == string.Empty &&
                r.Get("proxyPasswordDpapi") == proxyPasswordDpapi &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>(h =>
                h.TextEncoding == Encoding.UTF8 &&
                h.Decrypt(It.IsAny<string>()) == Encoding.UTF8.GetBytes(proxyPasswordDpapi));

            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual(proxyPasswordDpapi, config.ProxyPassword);
        }

        [Test]
        public async Task ConfigVariablesArePassedThroughCorrectly()
        {
            // Arrange

            var configHelper = Mock.Of<IConfigurationReader>(r =>
                r.Get("userid") == "BatchUserId" &&
                r.Get("password") == "BatchPassword" &&
                r.Get("url") == "BatchUrl" &&
                r.Get("storyid") == "BatchStoryId" &&
                r.Get("connectionString") == "BatchConnectionString" &&
                r.Get("queryString") == "BatchQueryString" &&
                r.Get("queryStringRels") == "BatchQueryStringRels" &&
                r.Get("unpublishItems") == "true" &&
                r.Get("proxy") == "BatchProxy" &&
                r.Get("proxyAnonymous") == "true" &&
                r.Get("proxyUsername") == "BatchProxyUsername" &&
                r.Get("proxyPassword") == "BatchProxyPassword" &&
                r.Get("dbType") == DatabaseStrings.SharpCloudExcel);

            var encryptionHelper = Mock.Of<IEncryptionHelper>();
            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var expectedConfig = new SharpCloudConfiguration
            {
                Username = "BatchUserId",
                Password = "BatchPassword",
                Url = "BatchUrl",
                ProxyUrl = "BatchProxy",
                ProxyUserName = "BatchProxyUsername",
                ProxyPassword = "BatchProxyPassword",
                UseDefaultProxyCredentials = true
            };

            var expectedSettings = new UpdateSettings
            {
                TargetStoryId = "BatchStoryId",
                QueryString = "BatchQueryString",
                QueryStringRels = "BatchQueryStringRels",
                ConnectionString = "BatchConnectionString",
                MaxRowCount = 1000,
                DBType = DatabaseType.SharpCloudExcel,
                UnpublishItems = true
            };

            var expectedConfigJson = JsonConvert.SerializeObject(expectedConfig);
            var expectedSettingsJson = JsonConvert.SerializeObject(expectedSettings);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            var settings = (UpdateSettings)configMock.Invocations[0].Arguments[1];

            var configJson = JsonConvert.SerializeObject(config);
            var settingsJson = JsonConvert.SerializeObject(settings);

            Assert.AreEqual(expectedConfigJson, configJson);
            Assert.AreEqual(expectedSettingsJson, settingsJson);
        }
    }
}
