using NUnit.Framework;
using SCQueryConnect.Models;
using System;

namespace SCQueryConnect.Tests.Models
{
    [TestFixture]
    public class QueryDataTests
    {
        [Test]
        public void DescriptionIsCopiedWithPrefix()
        {
            // Arrange

            var toCopy = new QueryData
            {
                Description = "Description"
            };

            // Act

            var copied = new QueryData(toCopy);

            // Assert

            Assert.AreEqual("Copy of Description", copied.Description);
        }

        [Test]
        public void CopiedDescriptionIsNotPrefixIfOriginallyEmpty()
        {
            // Arrange

            var toCopy = new QueryData();

            // Act

            var copied = new QueryData(toCopy);

            // Assert

            Assert.AreEqual(string.Empty, copied.Description);
        }

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
    }
}
