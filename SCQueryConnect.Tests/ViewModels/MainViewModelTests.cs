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
                messageService);

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
                messageService);

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
    }
}
