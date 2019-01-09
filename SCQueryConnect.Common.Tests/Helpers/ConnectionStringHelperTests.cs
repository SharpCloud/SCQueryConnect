using NUnit.Framework;
using SCQueryConnect.Common.Helpers;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class ConnectionStringHelperTests
    {
        [Test]
        public void ReturnsDataValue()
        {
            // Arrange

            var connectionString = "key1=value1;key2=value2;key3=value3;";
            var helper = new ConnectionStringHelper();

            // Act

            var output = helper.GetVariable(connectionString, "key2");

            // Assert

            Assert.AreEqual("value2", output);
        }
    }
}
