using NUnit.Framework;
using SCQueryConnect.Common.Helpers;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class DbConnectionFactoryTests
    {
        [TestCase(@"SourceId=12a8eb5d-c12b-4a8a-beeb-f67f3c168392;SourceUserName=;SourcePassword=;SourceServer=;Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\data.xlsx;Extended Properties='Excel 12.0 Xml;HDR = YES'")]
        [TestCase(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\data.xlsx;Extended Properties='Excel 12.0 Xml;HDR = YES';SourceId=12a8eb5d-c12b-4a8a-beeb-f67f3c168392")]
        public void ReturnsExcelConnectionStringFromSharpCloudConnectionString(string connectionString)
        {
            // Arrange

            var factory = new DbConnectionFactory();

            // Act

            var output = factory.GetDb(connectionString, DatabaseType.SharpCloudExcel);

            // Assert

            Assert.AreEqual(
                @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\data.xlsx;Extended Properties='Excel 12.0 Xml;HDR = YES'",
                output.ConnectionString);
        }
    }
}
