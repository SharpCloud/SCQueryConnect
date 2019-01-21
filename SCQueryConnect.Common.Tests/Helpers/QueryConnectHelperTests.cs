using Moq;
using NUnit.Framework;
using SC.Api.Interfaces;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System.Threading.Tasks;
using Category = SC.Entities.Models.Category;

namespace SCQueryConnect.Helpers.Tests.Helpers
{
    [TestFixture]
    public class MainWindowHelperTests
    {
        private const string StoryId = "5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";

        private QueryConnectHelper CreateQueryConnectHelper(
            IConnectionStringHelper connectionStringHelper = null,
            IDataChecker dataChecker = null,
            IDbConnectionFactory dbConnectionFactory = null,
            ILog log = null,
            IRelationshipsDataChecker relationshipsDataChecker = null,
            ISharpCloudApiFactory sharpCloudApiFactory = null)
        {
            return new QueryConnectHelper(
                connectionStringHelper ?? Mock.Of<IConnectionStringHelper>(),
                dataChecker ?? Mock.Of<IDataChecker>(),
                dbConnectionFactory ?? Mock.Of<IDbConnectionFactory>(),
                log ?? Mock.Of<ILog>(),
                relationshipsDataChecker ?? Mock.Of<IRelationshipsDataChecker>(),
                sharpCloudApiFactory ?? Mock.Of<ISharpCloudApiFactory>());
        }

        [Test]
        public void ReturnsStoryIdFromUrlWithView()
        {
            // Arrange

            var input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2/view/4e204f07-2598-469a-bdeb-583afd599cdc";
            var helper = CreateQueryConnectHelper();

            // Act

            var output = helper.GetStoryUrl(input);

            // Assert

            Assert.AreEqual(StoryId, output);
        }

        [Test]
        public void ReturnsStoryIdFromUrlWithoutView()
        {
            // Arrange

            var input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";
            var helper = CreateQueryConnectHelper();

            // Act

            var output = helper.GetStoryUrl(input);

            // Assert

            Assert.AreEqual(StoryId, output);
        }

        [Test]
        public void ReturnsStoryIdIfProvided()
        {
            // Arrange

            var input = "5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";
            var helper = CreateQueryConnectHelper();

            // Act

            var output = helper.GetStoryUrl(input);

            // Assert

            Assert.AreEqual(StoryId, output);
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
    }
}
