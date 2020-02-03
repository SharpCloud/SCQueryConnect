using NUnit.Framework;
using SCQueryConnect.Converters;

namespace SCQueryConnect.Tests.Converters
{
    [TestFixture]
    public class ValidExcelNameConverterTests
    {
        [TestCase(@"C:\Folder\MyFile", @"C:\Folder\MyFile.xlsx")]
        [TestCase(@"C:\Folder\MyFile.xls", @"C:\Folder\MyFile.xls")]
        [TestCase(@"C:\Folder\MyFile.xlsb", @"C:\Folder\MyFile.xlsb")]
        [TestCase(@"C:\Folder\MyFile.xlsm", @"C:\Folder\MyFile.xlsm")]
        [TestCase(@"C:\Folder\MyFile.XLSX", @"C:\Folder\MyFile.XLSX")]
        [TestCase(@"C:\Folder\MyFile.xlsx", @"C:\Folder\MyFile.xlsx")]
        [TestCase(@"C:\Folder\MyFile.xml", @"C:\Folder\MyFile.xml.xlsx")]
        public void ConvertsToValidFilename(string input, string expectedOut)
        {
            // Arrange

            var writer = new ValidExcelNameConverter();

            // Act

            var convertOutput = writer.Convert(input, null, null, null);
            var convertBackOutput = writer.ConvertBack(input, null, null, null);

            // Assert

            Assert.AreEqual(expectedOut, convertOutput);
            Assert.AreEqual(expectedOut, convertBackOutput);
        }
    }
}
