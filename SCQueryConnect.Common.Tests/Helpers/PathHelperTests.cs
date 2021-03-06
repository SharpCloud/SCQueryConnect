﻿using NUnit.Framework;
using SCQueryConnect.Common.Helpers;
using System.IO;
using System.Reflection;

namespace SCQueryConnect.Common.Tests.Helpers
{
    [TestFixture]
    public class PathHelperTests
    {
        [Test]
        public void ReturnsAbsolutePathWhenSpecified()
        {
            // Arrange

            var helper = new PathHelper();

            // Act

            var path = helper.GetAbsolutePath(@"C:\Directory\LogFile.txt");

            // Assert

            Assert.AreEqual(@"C:\Directory\LogFile.txt", path);
        }

        [Test]
        public void ReturnsAbsoluteVersionOfRelativePathWhenSpecified()
        {
            // Arrange

            var helper = new PathHelper();

            // Act

            var path = helper.GetAbsolutePath("LogFile.txt");

            // Assert

            var dllPath = Assembly.GetExecutingAssembly().Location;
            var dir = Path.GetDirectoryName(dllPath);
            var expected = Path.Combine(dir, "LogFile.txt");

            Assert.AreEqual(expected, path);
        }
    }
}
