using NUnit.Framework;
using SCQueryConnect.Converters;

namespace SCQueryConnect.Tests.Converters
{
    [TestFixture]
    public class UrlToStoryIdConverterTests
    {
        private const string StoryId = "5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";

        [Test]
        public void ReturnsStoryIdFromUrlWithView()
        {
            // Arrange

            const string input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2/view/4e204f07-2598-469a-bdeb-583afd599cdc";
            var converter = new UrlToStoryIdConverter();

            // Act

            var convertOutput = converter.Convert(input, null, null, null);
            var convertBackOutput = converter.ConvertBack(input, null, null, null);

            // Assert

            Assert.AreEqual(input, convertOutput);
            Assert.AreEqual(StoryId, convertBackOutput);
        }

        [Test]
        public void ReturnsStoryIdFromUrlWithoutView()
        {
            // Arrange

            const string input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";
            var converter = new UrlToStoryIdConverter();

            // Act

            var convertOutput = converter.Convert(input, null, null, null);
            var convertBackOutput = converter.ConvertBack(input, null, null, null);

            // Assert

            Assert.AreEqual(input, convertOutput);
            Assert.AreEqual(StoryId, convertBackOutput);
        }

        [Test]
        public void ReturnsStoryIdIfProvided()
        {
            // Arrange

            var converter = new UrlToStoryIdConverter();

            // Act

            var convertOutput = converter.Convert(StoryId, null, null, null);
            var convertBackOutput = converter.ConvertBack(StoryId, null, null, null);

            // Assert

            Assert.AreEqual(StoryId, convertOutput);
            Assert.AreEqual(StoryId, convertBackOutput);
        }
    }
}
