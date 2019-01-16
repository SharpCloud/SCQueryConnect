using NUnit.Framework;
using SCQueryConnect.Common.Helpers;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class ConnectionStringHelperTests
    {
        [TestCase("key1=value1;key2=value2;key3=value3;")]
        [TestCase("key1=value1;key2=value2")]
        public void ReturnsDataValue(string connectionString)
        {
            // Arrange

            var helper = new ConnectionStringHelper();

            // Act

            var output = helper.GetVariable(connectionString, "key2");

            // Assert

            Assert.AreEqual("value2", output);
        }

        [TestCase("key1=;")]
        [TestCase("key1=;key2=value2;")]
        public void ReturnsEmptyStringForNoValue(string connectionString)
        {
            // Arrange

            var helper = new ConnectionStringHelper();

            // Act

            var output = helper.GetVariable(connectionString, "key1");

            // Assert

            Assert.AreEqual(string.Empty, output);
        }

        [TestCase(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=;Extended Properties='Excel 12.0 Xml;HDR = YES'")]
        [TestCase(@"Provider=Microsoft.ACE.OLEDB.12.0;Extended Properties='Excel 12.0 Xml;HDR = YES';Data Source=")]
        [TestCase(@"Provider=Microsoft.ACE.OLEDB.12.0;Extended Properties='Excel 12.0 Xml;HDR = YES';Data Source=C:\OldLocation\OldData.xlsx")]
        public void ReturnsExcelConnectionStringFromSharpCloudConnectionString(string connectionString)
        {
            // Arrange

            const string newLocation = @"D:\NewLocation\NewData.xlsx";
            var helper = new ConnectionStringHelper();

            // Act

            var updated = helper.SetDataSource(connectionString, newLocation);

            // Assert

            var filename = helper.GetVariable(updated, "Data Source");
            Assert.AreEqual(newLocation, filename);
        }
    }
}
