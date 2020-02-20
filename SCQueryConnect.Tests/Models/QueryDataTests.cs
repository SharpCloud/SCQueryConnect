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

        [Test]
        public void AllQueriesAreCopiedWhenCopyingConnection()
        {
            // Arrange

            const string itemQuery = "ItemQuery";
            const string relationshipQuery = "RelationshipQuery";
            const string resourceUrlQuery = "ResourceUrlQuery";
            const string panelQuery = "PanelQuery";

            var original = new QueryData
            {
                QueryString = itemQuery,
                QueryStringRels = relationshipQuery,
                QueryStringResourceUrls = resourceUrlQuery,
                QueryStringPanels = panelQuery
            };

            // Act

            var copy = new QueryData(original);

            // Assert

            Assert.AreEqual(itemQuery, copy.QueryString);
            Assert.AreEqual(relationshipQuery, copy.QueryStringRels);
            Assert.AreEqual(resourceUrlQuery, copy.QueryStringResourceUrls);
            Assert.AreEqual(panelQuery, copy.QueryStringPanels);
        }

        [Test]
        public void CopiedConnectionsDoNotShareIds()
        {
            // Arrange

            var original = new QueryData();

            // Act

            var copy = new QueryData(original);

            // Assert

            Assert.AreNotEqual(original.Id, copy.Id);
        }
    }
}
