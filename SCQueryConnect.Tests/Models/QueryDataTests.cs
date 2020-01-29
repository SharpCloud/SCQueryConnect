using NUnit.Framework;
using SCQueryConnect.Models;
using System;

namespace SCQueryConnect.Tests.Models
{
    [TestFixture]
    public class QueryDataTests
    {
        private const string StoryId = "5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";

        [Test]
        public void IdIsGeneratedAndPersistent()
        {
            // Arrange

            var data = new QueryData();

            // Act

            var id1 = data.Id;
            var id2 = data.Id;

            // Assert

            Assert.IsNotEmpty(id1);
            Assert.AreEqual(id1, id2);

            var canParse = Guid.TryParse(id1, out _);
            Assert.IsTrue(canParse);
        }

        [Test]
        public void ReturnsStoryIdFromUrlWithView()
        {
            // Arrange

            var input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2/view/4e204f07-2598-469a-bdeb-583afd599cdc";
            var data = new QueryData();

            // Act

            data.StoryId = input;

            // Assert

            Assert.AreEqual(StoryId, data.StoryId);
        }

        [Test]
        public void ReturnsStoryIdFromUrlWithoutView()
        {
            // Arrange

            var input = "http://hostname.com/html/#/story/5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";
            var data = new QueryData();

            // Act

            data.StoryId = input;

            // Assert

            Assert.AreEqual(StoryId, data.StoryId);
        }

        [Test]
        public void ReturnsStoryIdIfProvided()
        {
            // Arrange

            var input = "5553cfec-bad2-4b60-96b6-b1e8c0aa7fe2";
            var data = new QueryData();

            // Act

            data.StoryId = input;

            // Assert

            Assert.AreEqual(StoryId, data.StoryId);
        }
    }
}
