using NUnit.Framework;
using SCQueryConnect.Common.Helpers;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class ExcelWriterTests
    {
        [TestCase(@"C:\Folder\MyFile", @"C:\Folder\MyFile.xlsx")]
        [TestCase(@"C:\Folder\MyFile.xls", @"C:\Folder\MyFile.xls")]
        [TestCase(@"C:\Folder\MyFile.xlsb", @"C:\Folder\MyFile.xlsb")]
        [TestCase(@"C:\Folder\MyFile.xlsm", @"C:\Folder\MyFile.xlsm")]
        [TestCase(@"C:\Folder\MyFile.xlsx", @"C:\Folder\MyFile.xlsx")]
        [TestCase(@"C:\Folder\MyFile.xml", @"C:\Folder\MyFile.xml.xlsx")]
        public void GetValidFilename(string input, string expectedOut)
        {
            // Arrange

            var writer = new ExcelWriter();

            // Act

            var output = writer.GetValidFilename(input);

            // Assert

            Assert.AreEqual(expectedOut, output);
        }
    }
}
