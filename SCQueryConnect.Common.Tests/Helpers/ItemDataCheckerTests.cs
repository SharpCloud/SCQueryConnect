using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class ItemDataCheckerTests
    {
        [TestCase(new object[] {"NAME", "Hello"})]
        [TestCase(new object[] {"Name", "World"})]
        [TestCase(new object[] {"External ID", "Foo"})]
        [TestCase(new object[] {"ExternalID", "bar"})]
        public async Task ValidationChecksAreCorrect(object[] values)
        {
            // Arrange

            var reader = Mock.Of<IDataReader>(r => r.FieldCount == values.Length);

            Mock.Get(reader)
                .Setup(r => r.GetName(It.IsAny<int>()))
                .Returns<int>(i => (string) values[i]);

            var log = Mock.Of<ILog>();
            var checker = new ItemDataChecker(log);

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

            var values = new[] {"Invalid", "Headings"};
            var reader = Mock.Of<IDataReader>(r => r.FieldCount == values.Length);

            Mock.Get(reader)
                .Setup(r => r.GetName(It.IsAny<int>()))
                .Returns<int>(i => values[i]);

            var log = Mock.Of<ILog>();
            var checker = new ItemDataChecker(log);

            // Act

            var result = await checker.CheckData(reader);

            // Assert

            Assert.IsFalse(result);

            Mock.Get(log).Verify(l =>
                l.LogWarning("Item data invalid - headings must contain one of 'Name', 'External ID', 'ExternalID'"));
        }
    }
}
