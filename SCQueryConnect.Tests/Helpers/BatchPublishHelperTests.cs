﻿using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.ViewModels;
using System.Collections.ObjectModel;
using System.Text;

namespace SCQueryConnect.Tests.Helpers
{
    [TestFixture]
    public class BatchPublishHelperTests
    {
        private const string outputRoot = "RoamingProfile";

        private static PublishSettings CreatePublishSettings(params string[] nestedConnectionNames)
        {
            var settings = new PublishSettings
            {
                Data = new QueryData
                {
                    ConnectionsString = string.Empty,
                    Name = nestedConnectionNames[0]
                },
                PasswordSecurity = PasswordSecurity.Base64,
                ProxyViewModel = new ProxyViewModel()
            };

            var last = settings.Data;

            for (var i = 1; i < nestedConnectionNames.Length; i++)
            {
                var name = nestedConnectionNames[i];
                var qd = new QueryData
                {
                    ConnectionsString = string.Empty,
                    Name = name
                };

                last.Connections = new ObservableCollection<QueryData>
                {
                    qd
                };

                last = qd;
            }

            return settings;
        }

        [Test]
        public void BatchFilesAreWrittenCorrectlyWithoutFolders()
        {
            // Arrange

            var encryptionHelper = Mock.Of<IEncryptionHelper>(e =>
                e.TextEncoding == Encoding.UTF8);

            var ioService = Mock.Of<IIOService>(s =>
                s.OutputRoot == outputRoot &&
                s.ReadAllTextFromFile(It.IsAny<string>()) == string.Empty);

            var passwordStorage = Mock.Of<IPasswordStorage>(p =>
                p.LoadPassword(It.IsAny<string>()) == string.Empty);

            var helper = new BatchPublishHelper(
                Mock.Of<IConnectionStringHelper>(),
                encryptionHelper,
                ioService,
                Mock.Of<IMessageService>(),
                passwordStorage);

            var settings = CreatePublishSettings("Connection");

            // Act

            helper.PublishBatchFolder(settings);

            // Assert

            var mock = Mock.Get(ioService);

            mock.Verify(s => s.WriteAllTextToFile(
                It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

            mock.Verify(s => s.WriteAllTextToFile(
                @"RoamingProfile\data\Connection\SCSQLBatch.exe.config",
                It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public void BatchFilesAreWrittenCorrectlyWithOneFolder()
        {
            // Arrange

            var encryptionHelper = Mock.Of<IEncryptionHelper>(e =>
                e.TextEncoding == Encoding.UTF8);

            var ioService = Mock.Of<IIOService>(s =>
                s.OutputRoot == outputRoot &&
                s.ReadAllTextFromFile(It.IsAny<string>()) == string.Empty);

            var passwordStorage = Mock.Of<IPasswordStorage>(p =>
                p.LoadPassword(It.IsAny<string>()) == string.Empty);

            var helper = new BatchPublishHelper(
                Mock.Of<IConnectionStringHelper>(),
                encryptionHelper,
                ioService,
                Mock.Of<IMessageService>(),
                passwordStorage);

            var settings = CreatePublishSettings("Folder1", "Connection");

            // Act

            helper.PublishBatchFolder(settings);

            // Assert

            var mock = Mock.Get(ioService);

            mock.Verify(s => s.WriteAllTextToFile(
                It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(2));

            mock.Verify(s => s.WriteAllTextToFile(
                @"RoamingProfile\data\Folder1\Connection\SCSQLBatch.exe.config",
                It.IsAny<string>()),
                Times.Once);

            mock.Verify(s => s.WriteAllTextToFile(
                @"RoamingProfile\data\Folder1\Folder1.bat",
                @"@echo off
echo - Running 'Connection'...
""RoamingProfile\data\Folder1\Connection\SCSQLBatch.exe""
"), Times.Once);
        }

        [Test]
        public void BatchFilesAreWrittenCorrectlyWithTwoFolders()
        {
            // Arrange

            var encryptionHelper = Mock.Of<IEncryptionHelper>(e =>
                e.TextEncoding == Encoding.UTF8);

            var ioService = Mock.Of<IIOService>(s =>
                s.OutputRoot == outputRoot &&
                s.ReadAllTextFromFile(It.IsAny<string>()) == string.Empty);

            var passwordStorage = Mock.Of<IPasswordStorage>(p =>
                p.LoadPassword(It.IsAny<string>()) == string.Empty);

            var helper = new BatchPublishHelper(
                Mock.Of<IConnectionStringHelper>(),
                encryptionHelper,
                ioService,
                Mock.Of<IMessageService>(),
                passwordStorage);

            var settings = CreatePublishSettings("Folder1", "Folder2", "Connection");

            // Act

            helper.PublishBatchFolder(settings);

            // Assert

            var mock = Mock.Get(ioService);

            mock.Verify(s => s.WriteAllTextToFile(
                It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(3));

            mock.Verify(s => s.WriteAllTextToFile(
                @"RoamingProfile\data\Folder1\Folder2\Connection\SCSQLBatch.exe.config",
                It.IsAny<string>()),
                Times.Once);

            mock.Verify(s => s.WriteAllTextToFile(
                @"RoamingProfile\data\Folder1\Folder2\Folder2.bat",
                @"@echo off
echo - Running 'Connection'...
""RoamingProfile\data\Folder1\Folder2\Connection\SCSQLBatch.exe""
"), Times.Once);

            mock.Verify(s => s.WriteAllTextToFile(
                @"RoamingProfile\data\Folder1\Folder1.bat",
                @"@echo off
echo - Running 'Folder2'...
call ""RoamingProfile\data\Folder1\Folder2\Folder2.bat""
"), Times.Once);
        }
    }
}
