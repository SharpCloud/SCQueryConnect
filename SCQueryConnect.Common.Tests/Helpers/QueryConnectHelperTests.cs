using Moq;
using NUnit.Framework;
using SC.Api.Interfaces;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using Category = SC.Entities.Models.Category;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class MainWindowHelperTests
    {
        private QueryConnectHelper CreateQueryConnectHelper(
            IArchitectureDetector architectureDetector = null,
            IConnectionStringHelper connectionStringHelper = null,
            IItemDataChecker itemDataChecker = null,
            IDbConnectionFactory dbConnectionFactory = null,
            IExcelWriter excelWriter = null,
            ILog log = null,
            IPanelsDataChecker panelsDataChecker = null,
            IRelationshipsDataChecker relationshipsDataChecker = null,
            IResourceUrlDataChecker resourceUrlDataChecker = null,
            ISharpCloudApiFactory sharpCloudApiFactory = null)
        {
            return new QueryConnectHelper(
                architectureDetector ?? Mock.Of<IArchitectureDetector>(),
                connectionStringHelper ?? Mock.Of<IConnectionStringHelper>(),
                itemDataChecker ?? Mock.Of<IItemDataChecker>(),
                dbConnectionFactory ?? Mock.Of<IDbConnectionFactory>(),
                excelWriter ?? Mock.Of<IExcelWriter>(),
                log ?? Mock.Of<ILog>(),
                panelsDataChecker ?? Mock.Of<IPanelsDataChecker>(),
                relationshipsDataChecker ?? Mock.Of<IRelationshipsDataChecker>(),
                resourceUrlDataChecker ?? Mock.Of<IResourceUrlDataChecker>(),
                sharpCloudApiFactory ?? Mock.Of<ISharpCloudApiFactory>());
        }

        [Test]
        public void StoryIsInvalidWithNoCategories()
        {
            // Arrange

            var story = new Story(new Roadmap(), Mock.Of<ISharpcloudClient2>());
            var helper = CreateQueryConnectHelper();

            // Act

            var isValid = helper.Validate(story, out var message);

            // Assert

            Assert.IsFalse(isValid);
            Assert.AreEqual(message, "Aborting update: story has no categories");
        }

        [Test]
        public void StoryIsValidWithCategories()
        {
            // Arrange

            var roadmap = new Roadmap();
            roadmap.Categories.Add(new Category());
            roadmap.Name = "StoryName";

            var story = new Story(roadmap, Mock.Of<ISharpcloudClient2>());
            var helper = CreateQueryConnectHelper();

            // Act

            var isValid = helper.Validate(story, out var message);

            // Assert

            Assert.IsTrue(isValid);
            Assert.AreEqual(message, "Reading story 'StoryName'");
        }

        [Test]
        public void InitialiseDatabaseThrowsIfCredentialsAreInvalid()
        {
            // Arrange

            var factory = Mock.Of<ISharpCloudApiFactory>(f =>
                f.CreateSharpCloudApi(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()) == null);

            var helper = CreateQueryConnectHelper(sharpCloudApiFactory: factory);

            // Act, Assert

            Assert.ThrowsAsync<InvalidCredentialsException>(() => helper.InitialiseDatabase(
                new SharpCloudConfiguration(),
                string.Empty,
                DatabaseType.SharpCloudExcel));
        }

        [TestCase(true, "32Bit(x86)")]
        [TestCase(false, "64Bit(AnyCPU)")]
        public void AppNameIsCorrect(bool simulateX86, string expectedAppSuffix)
        {
            // Arrange

            var detector = Mock.Of<IArchitectureDetector>(d =>
                d.Is32Bit == simulateX86);

            var helper = CreateQueryConnectHelper(architectureDetector: detector);

            // Act, Assert

            var suffixMatch = helper.AppName.EndsWith(expectedAppSuffix);
            Assert.IsTrue(suffixMatch);
        }
    }
}
