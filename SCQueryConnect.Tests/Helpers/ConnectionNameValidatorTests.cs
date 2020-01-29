using NUnit.Framework;
using SCQueryConnect.Helpers;
using System.Windows.Controls;

namespace SCQueryConnect.Tests.Helpers
{
    [TestFixture]
    public class ConnectionNameValidatorTests
    {
        [TestCase("Connection")]
        [TestCase("Connection Name")]
        public void ValidNamesPassValidation(string name)
        {
            // Arrange

            var validator = new ConnectionNameValidator();

            // Act

            var result = validator.Validate(name, null);

            // Assert

            Assert.AreEqual(ValidationResult.ValidResult, result);
        }

        [TestCase("My<Filename", "<")]
        [TestCase("My>Filename", ">")]
        [TestCase("My:Filename", ":")]
        [TestCase("My/Filename", "/")]
        [TestCase("My\\Filename", "\\")]
        [TestCase("My|Filename", "|")]
        [TestCase("My?Filename", "?")]
        [TestCase("My*Filename", "*")]
        public void InvalidCharactersAreDetected(string name, string reason)
        {
            // Arrange

            var validator = new ConnectionNameValidator();

            // Act

            var result = validator.Validate(name, null);

            // Assert

            var expected = $"Connection names cannot contain '{reason}'";
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(expected, result.ErrorContent);
        }

        [TestCase("CON")]
        [TestCase("PRN")]
        [TestCase("AUX")]
        [TestCase("NUL")]
        [TestCase("COM1")]
        [TestCase("COM2")]
        [TestCase("COM3")]
        [TestCase("COM4")]
        [TestCase("COM5")]
        [TestCase("COM6")]
        [TestCase("COM7")]
        [TestCase("COM8")]
        [TestCase("COM9")]
        [TestCase("LPT1")]
        [TestCase("LPT2")]
        [TestCase("LPT3")]
        [TestCase("LPT4")]
        [TestCase("LPT5")]
        [TestCase("LPT6")]
        [TestCase("LPT7")]
        [TestCase("LPT8")]
        [TestCase("LPT9")]
        public void InvalidConnectionNamesAreDetected(string name)
        {
            // Arrange

            var validator = new ConnectionNameValidator();

            // Act

            var result = validator.Validate(name, null);

            // Assert

            var expected = $"The Name '{name}' is not valid";
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(expected, result.ErrorContent);
        }

        [Test]
        public void ConnectionNamesCannotEndWithDot()
        {
            // Arrange

            var validator = new ConnectionNameValidator();

            // Act

            var result = validator.Validate("Connection.", null);

            // Assert

            const string expected = "Names cannot end with '.'";
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(expected, result.ErrorContent);
        }
    }
}
