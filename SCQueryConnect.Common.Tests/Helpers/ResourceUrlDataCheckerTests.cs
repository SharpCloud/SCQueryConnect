using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class ResourceUrlDataCheckerTests
    {
        private static IDataReader CreateDataReader(params string[] values)
        {
            var reader = Mock.Of<IDataReader>(r => r.FieldCount == values.Length);

            Mock.Get(reader)
                .Setup(r => r.GetName(It.IsAny<int>()))
                .Returns<int>(i => values[i]);

            return reader;
        }

        [Test]
        public async Task DataIsValidWithAllHeadings()
        {
            // Arrange

            var reader = CreateDataReader("ExternalId", "ResourceName", "Description", "URL");
            var log = Mock.Of<ILog>();
            var checker = new ResourceUrlDataChecker(log);

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

            var reader = CreateDataReader("ExternalID", "ResourceName", "URL");
            var log = Mock.Of<ILog>();
            var checker = new ResourceUrlDataChecker(log);

            // Act

            var result = await checker.CheckData(reader);

            // Assert

            Assert.IsFalse(result);

            Mock.Get(log).Verify(l =>
                l.LogWarning("Resource URL data invalid - headings must contain all of 'ExternalID', 'ResourceName', 'Description', 'URL'"));
        }
    }
}
