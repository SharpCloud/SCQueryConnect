using System.Collections.Generic;
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
        private const string UserId = "userid";
        private const string Password = "password";
        private const string Password64 = "password64";
        private const string PasswordDpapi = "passwordDpapi";
        private const string Url = "url";
        private const string StoryId = "storyid";
        private const string ConnectionString = "connectionString";
        private const string QueryString = "queryString";
        private const string QueryStringRels = "queryStringRels";
        private const string UnpublishItems = "unpublishItems";
        private const string Proxy = "proxy";
        private const string ProxyAnonymous = "proxyAnonymous";
        private const string ProxyUsername = "proxyUsername";
        private const string ProxyPassword = "proxyPassword";
        private const string ProxyPassword64 = "proxyPassword64";
        private const string ProxyPasswordDpapi = "proxyPasswordDpapi";
        private const string DbType = "dbType";

        private IConfigurationReader CreateConfigurationReader(
            IDictionary<string, string> configOverrides = null)
        {
            var defaultConfig = new Dictionary<string, string>
            {
                [UserId] = "BatchUserId",
                [Password] = "BatchPassword",
                [Password64] = "QmF0Y2hQYXNzd29yZDY0", // 'BatchPassword64'
                [PasswordDpapi] = "BatchPasswordDpapi",
                [Url] = "BatchUrl",
                [StoryId] = "BatchStoryId",
                [ConnectionString] = "BatchConnectionString",
                [QueryString] = "BatchQueryString",
                [QueryStringRels] = "BatchQueryStringRels",
                [UnpublishItems] = "false",
                [Proxy] = "BatchProxy",
                [ProxyAnonymous] = "false",
                [ProxyUsername] = "BatchProxyUsername",
                [ProxyPassword] = "BatchProxyPassword",
                [ProxyPassword64] = "QmF0Y2hQcm94eVBhc3N3b3JkNjQ=", // 'BatchProxyPassword64'
                [ProxyPasswordDpapi] = "BatchProxyPasswordDpapi",
                [DbType] = DatabaseStrings.SharpCloudExcel
            };

            var configReaderMock = new Mock<IConfigurationReader>();

            foreach (var configKey in defaultConfig.Keys)
            {
                string configValue;

                if (configOverrides != null &&
                    configOverrides.ContainsKey(configKey))
                {
                    configValue = configOverrides[configKey];
                }
                else
                {
                    configValue = defaultConfig[configKey];
                }

                configReaderMock
                    .Setup(r => r.Get(configKey))
                    .Returns(configValue);
            }

            return configReaderMock.Object;
        }

        [Test]
        public async Task PreferPasswordIfPresent()
        {
            // Arrange

            var configHelper = CreateConfigurationReader();
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

            var overrides = new Dictionary<string, string>
            {
                [Password] = string.Empty
            };

            var configHelper = CreateConfigurationReader(overrides);
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

            var overrides = new Dictionary<string, string>
            {
                [Password] = string.Empty,
                [PasswordDpapi] = string.Empty
            };

            var configHelper = CreateConfigurationReader(overrides);
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

            const string passwordDpapiValue = "BatchPasswordDpapi";

            var overrides = new Dictionary<string, string>
            {
                [Password] = string.Empty,
                [Password64] = string.Empty,
                [PasswordDpapi] = passwordDpapiValue
            };

            var configHelper = CreateConfigurationReader(overrides);

            var encryptionHelper = Mock.Of<IEncryptionHelper>(h =>
                h.TextEncoding == Encoding.UTF8 &&
                h.Decrypt(It.IsAny<string>()) == Encoding.UTF8.GetBytes(passwordDpapiValue));

            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual(passwordDpapiValue, config.Password);
        }

        [Test]
        public async Task PreferProxyPasswordIfPresent()
        {
            // Arrange

            var configHelper = CreateConfigurationReader();
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

            var overrides = new Dictionary<string, string>
            {
                [ProxyPassword] = string.Empty
            };

            var configHelper = CreateConfigurationReader(overrides);
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

            var overrides = new Dictionary<string, string>
            {
                [ProxyPassword] = string.Empty,
                [ProxyPasswordDpapi] = string.Empty
            };

            var configHelper = CreateConfigurationReader(overrides);
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

            const string proxyPasswordDpapiValue = "BatchProxyPasswordDpapi";

            var overrides = new Dictionary<string, string>
            {
                [ProxyPassword] = string.Empty,
                [ProxyPassword64] = string.Empty,
                [ProxyPasswordDpapi] = proxyPasswordDpapiValue
            };

            var configHelper = CreateConfigurationReader(overrides);

            var encryptionHelper = Mock.Of<IEncryptionHelper>(h =>
                h.TextEncoding == Encoding.UTF8 &&
                h.Decrypt(It.IsAny<string>()) == Encoding.UTF8.GetBytes(proxyPasswordDpapiValue));

            var logger = Mock.Of<ILog>();
            var qcHelper = Mock.Of<IQueryConnectHelper>();

            var logic = new BatchLogic(configHelper, encryptionHelper, logger, qcHelper);

            // Act

            await logic.Run();

            // Assert

            var configMock = Mock.Get(qcHelper);
            Assert.AreEqual(1, configMock.Invocations.Count);

            var config = (SharpCloudConfiguration)configMock.Invocations[0].Arguments[0];
            Assert.AreEqual(proxyPasswordDpapiValue, config.ProxyPassword);
        }

        [Test]
        public async Task ConfigVariablesArePassedThroughCorrectly()
        {
            // Arrange


            var overrides = new Dictionary<string, string>
            {
                [Password64] = null,
                [PasswordDpapi] = null,
                [UnpublishItems] = "true",
                [ProxyAnonymous] = "true",
                [ProxyPassword64] = null,
                [ProxyPasswordDpapi] = null
            };

            var configHelper = CreateConfigurationReader(overrides);
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
