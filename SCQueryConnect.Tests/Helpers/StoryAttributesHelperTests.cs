using Moq;
using NUnit.Framework;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Tests.Helpers
{
    [TestFixture]
    public class StoryAttributesHelperTests
    {
        [Test]
        public async Task AttributeMappingsExcludeNonCustomAttributes()
        {
            // Arrange

            const string custom = "Custom";
            const string nonCustom = "NonCustom";

            var table = new DataTable();
            table.Columns.Add(custom);
            table.Columns.Add(nonCustom);

            var viewModel = Mock.Of<IMainViewModel>(vm =>
                vm.PreviewSql(
                    It.IsAny<QueryData>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryEntityType>()) == Task.FromResult(table) &&
                vm.SelectedQueryData == new QueryData());

            var qcHelper = Mock.Of<IQueryConnectHelper>(h =>
                h.IsCustomAttribute(custom) == true &&
                h.IsCustomAttribute(nonCustom) == false);

            var helper = new StoryAttributesHelper(
                viewModel,
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>(),
                qcHelper,
                Mock.Of<ISharpCloudApiFactory>());

            var defaultDesignations = new AttributeDesignations();

            // Act

            var mappings =  await helper.GetAttributeMappings(defaultDesignations);

            // Assert

            Assert.AreEqual(1, mappings.Count);
            Assert.AreEqual(custom, mappings[0].SourceName);
            Assert.AreEqual(defaultDesignations, mappings[0].Target);
        }
    }
}
