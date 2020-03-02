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

            var table = new DataTable();

            var inputAttributes = new[]
            {
                "INTERNALID",
                "INTERNAL ID",
                "NAME",
                "EXTERNALID",
                "EXTERNAL ID",
                "DESCRIPTION",
                "START",
                "CATEGORY",
                "DURATION DAYS",
                "DURATION (DAYS)",
                "DURATION",
                "CLICKACTIONURL",
                "IMAGE",
                "PUBLISHED",
                "LIKES",
                "DISLIKES",
                "TAGS",
                "TAGS.MyTag",
                "ROWID",
                "Attribute"
            };

            foreach (var headingName in inputAttributes)
            {
                table.Columns.Add(headingName);
            }

            var viewModel = Mock.Of<IMainViewModel>(vm =>
                vm.PreviewSql(
                    It.IsAny<QueryData>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryEntityType>()) == Task.FromResult(table) &&
                vm.SelectedQueryData == new QueryData());

            var helper = new StoryAttributesHelper(
                viewModel,
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordStorage>(),
                Mock.Of<IProxyViewModel>(),
                Mock.Of<ISharpCloudApiFactory>());

            var defaultDesignations = new AttributeDesignations();

            // Act

            var mappings =  await helper.GetAttributeMappings(defaultDesignations);

            // Assert

            Assert.AreEqual(1, mappings.Count);
            Assert.AreEqual("Attribute", mappings[0].SourceName);
            Assert.AreEqual(defaultDesignations, mappings[0].Target);
        }
    }
}
