using NUnit.Framework;

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
            var helper = new MainWindowHelper();

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
            var helper = new MainWindowHelper();

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
            var helper = new MainWindowHelper();

            // Act

            var output = helper.GetStoryUrl(input);

            // Assert

            Assert.AreEqual(StoryId, output);
        }
    }
}
