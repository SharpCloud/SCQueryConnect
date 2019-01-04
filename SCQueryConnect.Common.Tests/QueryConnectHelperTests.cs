using Moq;
using NUnit.Framework;
using SC.Api.Interfaces;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCQueryConnect.Common;
using Category = SC.Entities.Models.Category;

namespace SCQueryConnect.Helpers.Tests
{
    [TestFixture]
    public class MainWindowHelperTests
    {
        private const string StoryId = "5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";

        [Test]
        public void ReturnsStoryIdFromUrlWithView()
        {
            // Arrange

            var input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2/view/4e204f07-2598-469a-bdeb-583afd599cdc";
            var helper = new QueryConnectHelper();

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
            var helper = new QueryConnectHelper();

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
            var helper = new QueryConnectHelper();

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
            var helper = new QueryConnectHelper();

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
            var helper = new QueryConnectHelper();

            // Act

            var isValid = helper.Validate(story, out var message);

            // Assert

            Assert.IsTrue(isValid);
            Assert.AreEqual(message, "Reading story 'StoryName'");
        }
    }
}
