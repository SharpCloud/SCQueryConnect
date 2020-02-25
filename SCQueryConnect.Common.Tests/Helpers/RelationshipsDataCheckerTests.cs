using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class RelationshipsDataCheckerTests
    {
        [TestCase(new object[] { "Item1", "Item2"})]
        [TestCase(new object[] { "ITEM 1", "ITEM 2" })]
        [TestCase(new object[] { "ExternalID1", "EXTERNALID2" })]
        [TestCase(new object[] { "EXTERNALID 1", "ExternalID 2" })]
        [TestCase(new object[] { "EXTERNAL ID 1", "External ID 2" })]
        [TestCase(new object[] { "Internal ID 1", "INTERNAL ID 2" })]
        public async Task ValidationChecksAreCorrect(object[] values)
        {
            // Arrange

            var allValues = values.Cast<string>().Concat(new[] {"comment", "DIRECTION"}).ToArray();
            var reader = Mock.Of<IDataReader>(r => r.FieldCount == allValues.Length);

            Mock.Get(reader)
                .Setup(r => r.GetName(It.IsAny<int>()))
                .Returns<int>(i => allValues[i]);

            var log = Mock.Of<ILog>();
            var checker = new RelationshipsDataChecker(log);

            // Act

            var result = await checker.CheckData(reader);

            // Assert

            Assert.IsTrue(result);

            Mock.Get(log).Verify(l => l.Log(It.IsAny<string>()), Times.Never);
            Mock.Get(log).Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            Mock.Get(log).Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ValidationCheckLogsWithInvalidHeadings()
        {
            // Arrange

            var values = new[] { "Invalid", "Headings" };
            var reader = Mock.Of<IDataReader>(r => r.FieldCount == values.Length);

            Mock.Get(reader)
                .Setup(r => r.GetName(It.IsAny<int>()))
                .Returns<int>(i => values[i]);

            var log = Mock.Of<ILog>();
            var checker = new RelationshipsDataChecker(log);

            // Act

            var result = await checker.CheckData(reader);

            // Assert

            Assert.IsFalse(result);

            Mock.Get(log).Verify(l =>
                l.LogWarning("Relationships data invalid - headings must contain one of ['Item1', 'Item 1', 'ExternalID1', 'ExternalID 1', 'External ID 1', 'Internal ID 1']; one of ['Item2', 'Item 2', 'ExternalID2', 'ExternalID 2', 'External ID 2', 'Internal ID 2']; 'Comment'; 'Direction'"));
        }
    }
}
