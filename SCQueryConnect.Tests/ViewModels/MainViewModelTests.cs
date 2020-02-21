using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.ViewModels;
using System.Data;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class MainViewModelTests
    {
        private const string PanelTypeHeader = "PanelType";
        private const string V3ConnectionsPath = "V3ConnectionsPath";
        private const string V3ConnectionsBackupPath = "V3ConnectionsBackupPath";
        private const string V4ConnectionsPath = "V4ConnectionsPath";

        [TestCase("Attribute")]
        [TestCase("CustomResource")]
        [TestCase("HTML")]
        [TestCase("Image")]
        [TestCase("RichText")]
        [TestCase("Video")]
        [TestCase("richtext")]
        public void ValidatePanelDataDoesNotShowsMessageForValidTypes(string panelType)
        {
            // Arrange

            var messageService = Mock.Of<IMessageService>();

            var vm = new MainViewModel(
                Mock.Of<IEncryptionHelper>(),
                Mock.Of<IIOService>(),
                messageService,
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>());

            var table = new DataTable();
            table.Columns.Add(PanelTypeHeader);

            var row = table.NewRow();
            row[PanelTypeHeader] = panelType;
            table.Rows.Add(row);

            var data = new QueryData
            {
                QueryResultsPanels = table
            };

            // Act

            vm.ValidatePanelData(data);

            // Assert

            Mock.Get(messageService).Verify(ms =>
                ms.Show(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ValidatePanelDataShowsMessageForInvalidTypes()
        {
            // Arrange

            const string panelTypeHeader = "PanelType";
            const string invalidPanelType = "Invalid Type";

            var messageService = Mock.Of<IMessageService>();

            var vm = new MainViewModel(
                Mock.Of<IEncryptionHelper>(),
                Mock.Of<IIOService>(),
                messageService,
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>());

            var table = new DataTable();
            table.Columns.Add(panelTypeHeader);

            var row = table.NewRow();
            row[panelTypeHeader] = invalidPanelType;
            table.Rows.Add(row);

            var data = new QueryData
            {
                QueryResultsPanels = table
            };

            // Act

            vm.ValidatePanelData(data);

            // Assert

            Mock.Get(messageService).Verify(ms =>
                ms.Show(It.Is<string>(s => s.Contains(invalidPanelType))));
        }

        [Test]
        public void MigratesConnectionsIfOnlyV3FoundAndDoesNotCreateExamples()
        {
            // Arrange

            var ioService = Mock.Of<IIOService>(s =>
                s.V3ConnectionsPath == V3ConnectionsPath &&
                s.V3ConnectionsBackupPath == V3ConnectionsBackupPath &&
                s.FileExists(V3ConnectionsPath) == true &&
                s.FileExists(V3ConnectionsBackupPath) == true &&
                s.ReadAllTextFromFile(V3ConnectionsBackupPath) == "[]");

            var vm = new MainViewModel(
                Mock.Of<IEncryptionHelper>(),
                ioService,
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>());

            // Act

            vm.LoadApplicationState();

            // Assert

            Mock.Get(ioService).Verify(s =>
                s.MoveFile(V3ConnectionsPath, V3ConnectionsBackupPath),
                Times.Once);

            Mock.Get(ioService).Verify(s =>
                s.ReadAllTextFromFile(V3ConnectionsBackupPath),
                Times.Once);

            Mock.Get(ioService).Verify(s =>
                s.ReadAllTextFromFile(V4ConnectionsPath),
                Times.Never);

            Assert.IsEmpty(vm.Connections);
        }

        [Test]
        public void DoesNotMigrateIfBothV3AndV4FoundAndDoesNotCreateExamples()
        {
            // Arrange

            var ioService = Mock.Of<IIOService>(s =>
                s.V3ConnectionsPath == V3ConnectionsPath &&
                s.V3ConnectionsBackupPath == V3ConnectionsBackupPath &&
                s.V4ConnectionsPath == V4ConnectionsPath &&
                s.FileExists(V3ConnectionsPath) == true &&
                s.FileExists(V3ConnectionsBackupPath) == true &&
                s.FileExists(V4ConnectionsPath) == true &&
                s.ReadAllTextFromFile(V4ConnectionsPath) == "{ Connections: [] }");

            var vm = new MainViewModel(
                Mock.Of<IEncryptionHelper>(),
                ioService,
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>());

            // Act

            vm.LoadApplicationState();

            // Assert

            Mock.Get(ioService).Verify(s =>
                s.MoveFile(V3ConnectionsPath, V3ConnectionsBackupPath),
                Times.Never);

            Mock.Get(ioService).Verify(s =>
                s.ReadAllTextFromFile(V3ConnectionsBackupPath),
                Times.Never);

            Mock.Get(ioService).Verify(s =>
                s.ReadAllTextFromFile(V4ConnectionsPath),
                Times.Once);

            Assert.IsEmpty(vm.Connections);
        }

        [Test]
        public void CreatesExamplesIfV3AndV4NotFound()
        {
            // Arrange

            var ioService = Mock.Of<IIOService>();

            var vm = new MainViewModel(
                Mock.Of<IEncryptionHelper>(),
                ioService,
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>());

            // Act

            vm.LoadApplicationState();

            // Assert

            Assert.AreEqual(1, vm.Connections.Count);
            Assert.AreEqual("My Connections", vm.Connections[0].Name);
            Assert.AreEqual("Excel Example", vm.Connections[0].Connections[0].Name);
        }
    }
}
