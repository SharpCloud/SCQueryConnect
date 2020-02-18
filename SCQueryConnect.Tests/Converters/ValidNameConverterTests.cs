using NUnit.Framework;
using SCQueryConnect.Converters;

namespace SCQueryConnect.Tests.Converters
{
    [TestFixture]
    public class ValidNameConverterTests
    {
        [TestCase(@"C:\Folder\MyFile", @"C:\Folder\MyFile.xlsx")]
        [TestCase(@"C:\Folder\MyFile.xls", @"C:\Folder\MyFile.xls")]
        [TestCase(@"C:\Folder\MyFile.xlsb", @"C:\Folder\MyFile.xlsb")]
        [TestCase(@"C:\Folder\MyFile.xlsm", @"C:\Folder\MyFile.xlsm")]
        [TestCase(@"C:\Folder\MyFile.XLSX", @"C:\Folder\MyFile.XLSX")]
        [TestCase(@"C:\Folder\MyFile.xlsx", @"C:\Folder\MyFile.xlsx")]
        [TestCase(@"C:\Folder\MyFile.xml", @"C:\Folder\MyFile.xml.xlsx")]
        public void ConvertsToValidExcelFilename(string input, string expectedOut)
        {
            // Arrange

            var writer = new ValidNameConverter();

            // Act

            var convertOutput = writer.Convert(input, null, "Excel", null);
            var convertBackOutput = writer.ConvertBack(input, null, "Excel", null);

            // Assert

            Assert.AreEqual(expectedOut, convertOutput);
            Assert.AreEqual(expectedOut, convertBackOutput);
        }

        [TestCase(@"C:\Folder\MyFile", @"C:\Folder\MyFile.accdb")]
        [TestCase(@"C:\Folder\MyFile.accdb", @"C:\Folder\MyFile.accdb")]
        [TestCase(@"C:\Folder\MyFile.ACCDB", @"C:\Folder\MyFile.ACCDB")]
        [TestCase(@"C:\Folder\MyFile.xml", @"C:\Folder\MyFile.xml.accdb")]
        public void ConvertsToValidAccessFilename(string input, string expectedOut)
        {
            // Arrange

            var writer = new ValidNameConverter();

            // Act

            var convertOutput = writer.Convert(input, null, "Access", null);
            var convertBackOutput = writer.ConvertBack(input, null, "Access", null);

            // Assert

            Assert.AreEqual(expectedOut, convertOutput);
            Assert.AreEqual(expectedOut, convertBackOutput);
        }
    }
}
