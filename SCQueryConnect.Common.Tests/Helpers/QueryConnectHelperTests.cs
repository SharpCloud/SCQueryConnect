using Moq;
using NUnit.Framework;
using SC.Api.Interfaces;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
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
            IDbConnectionFactory dbConnectionFactory = null,
            IExcelWriter excelWriter = null,
            ILog log = null,
            IRelationshipsBuilder relationshipsBuilder = null,
            ISharpCloudApiFactory sharpCloudApiFactory = null,
            params IDataChecker[] dataCheckers)
        {
            return new QueryConnectHelper(
                architectureDetector ?? Mock.Of<IArchitectureDetector>(),
                connectionStringHelper ?? Mock.Of<IConnectionStringHelper>(),
                dataCheckers,
                dbConnectionFactory ?? Mock.Of<IDbConnectionFactory>(),
                excelWriter ?? Mock.Of<IExcelWriter>(),
                log ?? Mock.Of<ILog>(),
                relationshipsBuilder ?? Mock.Of<IRelationshipsBuilder>(),
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

        [TestCase("INTERNALID", false)]
        [TestCase("INTERNAL ID", false)]
        [TestCase("NAME", false)]
        [TestCase("EXTERNALID", false)]
        [TestCase("EXTERNAL ID", false)]
        [TestCase("DESCRIPTION", false)]
        [TestCase("START", false)]
        [TestCase("CATEGORY", false)]
        [TestCase("DURATION DAYS", false)]
        [TestCase("DURATION (DAYS)", false)]
        [TestCase("DURATION", false)]
        [TestCase("CLICKACTIONURL", false)]
        [TestCase("IMAGE", false)]
        [TestCase("PUBLISHED", false)]
        [TestCase("LIKES", false)]
        [TestCase("DISLIKES", false)]
        [TestCase("TAGS", false)]
        [TestCase("TAGS.MyTag", false)]
        [TestCase("ROWID", false)]
        [TestCase("Attribute", true)]
        public void CustomAttributesAreKnown(string name, bool expected)
        {
            // Arrange

            var helper = CreateQueryConnectHelper();

            // Act

            var result = helper.IsCustomAttribute(name);

            // Assert

            Assert.AreEqual(expected, result);
        }
    }
}
