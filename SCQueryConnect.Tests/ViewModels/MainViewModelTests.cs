using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Logging;
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

        private MainViewModel CreateViewModel(
            IBatchPublishHelper batchPublishHelper = null,
            IDbConnectionFactory dbConnectionFactory = null,
            IEncryptionHelper encryptionHelper = null,
            IIOService ioService = null,
            IItemsDataChecker itemsDataChecker = null,
            ILog log = null,
            IMessageService messageService = null,
            IPanelsDataChecker panelsDataChecker = null,
            IPasswordStorage passwordStorage = null,
            IProxyViewModel proxyViewModel = null,
            IQueryConnectHelper queryConnectHelper = null,
            IRelationshipsDataChecker relationshipsDataChecker = null,
            IResourceUrlsDataChecker resourceUrlsDataChecker = null,
            ISaveFileDialogService saveFileDialogService = null)
        {
            var vm = new MainViewModel(
                batchPublishHelper ?? Mock.Of<IBatchPublishHelper>(),
                dbConnectionFactory ?? Mock.Of<IDbConnectionFactory>(),
                encryptionHelper ?? Mock.Of<IEncryptionHelper>(),
                ioService ?? Mock.Of<IIOService>(),
                itemsDataChecker ?? Mock.Of<IItemsDataChecker>(),
                log ?? new MultiDestinationLogger(),
                messageService ?? Mock.Of<IMessageService>(),
                panelsDataChecker ?? Mock.Of<IPanelsDataChecker>(),
                passwordStorage ?? Mock.Of<IPasswordStorage>(),
                proxyViewModel ?? Mock.Of<IProxyViewModel>(),
                queryConnectHelper ?? Mock.Of<IQueryConnectHelper>(),
                relationshipsDataChecker ?? Mock.Of<IRelationshipsDataChecker>(),
                resourceUrlsDataChecker ?? Mock.Of<IResourceUrlsDataChecker>(),
                saveFileDialogService ?? Mock.Of<ISaveFileDialogService>());

            return vm;
        }

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
            var vm = CreateViewModel(messageService: messageService);

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

            const string invalidPanelType = "Invalid Type";

            var messageService = Mock.Of<IMessageService>();
            var vm = CreateViewModel(messageService: messageService);

            var table = new DataTable();
            table.Columns.Add(PanelTypeHeader);

            var row = table.NewRow();
            row[PanelTypeHeader] = invalidPanelType;
            table.Rows.Add(row);

            var data = new QueryData
            {
                QueryResultsPanels = table
            };

            // Act

            vm.ValidatePanelData(data);

            // Assert

            Mock.Get(messageService).Verify(ms =>
                ms.Show(It.Is<string>(s => s.Contains(invalidPanelType) &&
                                           !s.Contains("unspecified panel"))));
        }

        [Test]
        public void ValidatePanelDataShowsMessageForUnspecifiedTypes()
        {
            // Arrange

            var messageService = Mock.Of<IMessageService>();
            var vm = CreateViewModel(messageService: messageService);

            var table = new DataTable();
            table.Columns.Add(PanelTypeHeader);

            var nullTypeRow = table.NewRow();
            table.Rows.Add(nullTypeRow);

            var emptyTypeRow = table.NewRow();
            emptyTypeRow[PanelTypeHeader] = string.Empty;
            table.Rows.Add(emptyTypeRow);

            var data = new QueryData
            {
                QueryResultsPanels = table
            };

            // Act

            vm.ValidatePanelData(data);

            // Assert

            Mock.Get(messageService).Verify(ms =>
                ms.Show(It.Is<string>(s => s == "Data contains 2 unspecified panel type(s)")));
        }

        [Test]
        public void ValidatePanelDataShowsMessageForInvalidAndUnspecifiedTypes()
        {
            // Arrange

            const string invalidPanelType = "Invalid Type";

            var messageService = Mock.Of<IMessageService>();
            var vm = CreateViewModel(messageService: messageService);

            var table = new DataTable();
            table.Columns.Add(PanelTypeHeader);

            var unspecifiedRow = table.NewRow();
            table.Rows.Add(unspecifiedRow);

            var invalidRow = table.NewRow();
            invalidRow[PanelTypeHeader] = invalidPanelType;
            table.Rows.Add(invalidRow);

            var data = new QueryData
            {
                QueryResultsPanels = table
            };

            // Act

            vm.ValidatePanelData(data);

            // Assert

            Mock.Get(messageService).Verify(ms =>
                ms.Show(It.Is<string>(s => s.Contains(invalidPanelType) &&
                                           s.Contains("Data contains 1 unspecified panel type(s)"))));
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

            var vm = CreateViewModel(ioService: ioService);

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

            var vm = CreateViewModel(ioService: ioService);

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
            var vm = CreateViewModel(ioService: ioService);

            // Act

            vm.LoadApplicationState();

            // Assert

            Assert.AreEqual(1, vm.Connections.Count);
            Assert.AreEqual("My Connections", vm.Connections[0].Name);
            Assert.AreEqual("Excel Example", vm.Connections[0].Connections[0].Name);
        }
    }
}
