using Moq;
using NUnit.Framework;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class AttributeMappingEditorViewModelTests
    {
        private const string Attribute1 = "Attribute1";
        private const string AttributeId1 = "AttributeId1";
        private const string Attribute2 = "Attribute2";
        private const string AttributeId2 = "AttributeId2";
        private const string Attribute3 = "Attribute3";
        private const string AttributeId3 = "AttributeId3";

        [Test]
        public async Task ValidMappingsAreNotMarkedAsBroken()
        {
            // Arrange

            var fromSql = new List<AttributeMapping>
            {
                new AttributeMapping
                {
                    SourceName = Attribute1
                }
            };

            var mapping = new Dictionary<string, string>
            {
                [Attribute1] = AttributeId1
            };

            var fromStory = new List<AttributeDesignations>
            {
                new AttributeDesignations
                {
                    Id = AttributeId1,
                    Name = Attribute1
                }
            };

            var helper = Mock.Of<IStoryAttributesHelper>(h =>
                h.GetAttributeMappings(It.IsAny<AttributeDesignations>()) == Task.FromResult(fromSql) &&
                h.GetStoryAttributes(It.IsAny<AttributeDesignations>()) == Task.FromResult(fromStory));

            var vm = new AttributeMappingEditorViewModel(helper);

            // Act

            await vm.InitialiseEditor(mapping);

            // Assert

            Assert.AreEqual(1, vm.AttributeMappings.Count);
            Assert.AreEqual(Attribute1, vm.AttributeMappings[0].SourceName);
            Assert.IsFalse(vm.AttributeMappings[0].IsBrokenMapping);
        }

        [Test]
        public async Task NamesInDataFromSqlMissingInMappingAreMarkedAsBrokenMappings()
        {
            // Arrange

            var fromSql = new List<AttributeMapping>
            {
                new AttributeMapping
                {
                    SourceName = Attribute1
                }
            };

            var mapping = new Dictionary<string, string>();

            var fromStory = new List<AttributeDesignations>
            {
                new AttributeDesignations
                {
                    Id = AttributeId1,
                    Name = Attribute1
                }
            };

            var helper = Mock.Of<IStoryAttributesHelper>(h =>
                h.GetAttributeMappings(It.IsAny<AttributeDesignations>()) == Task.FromResult(fromSql) &&
                h.GetStoryAttributes(It.IsAny<AttributeDesignations>()) == Task.FromResult(fromStory));

            var vm = new AttributeMappingEditorViewModel(helper);

            // Act

            await vm.InitialiseEditor(mapping);

            // Assert

            Assert.AreEqual(1, vm.AttributeMappings.Count);
            Assert.AreEqual(Attribute1, vm.AttributeMappings[0].SourceName);
            Assert.IsTrue(vm.AttributeMappings[0].IsBrokenMapping);
        }

        [Test]
        public async Task NamesInMappingMissingInStoryAttributesAreMarkedAsBrokenMappings()
        {
            // Arrange

            var fromSql = new List<AttributeMapping>
            {
                new AttributeMapping
                {
                    SourceName = Attribute1
                }
            };

            var mapping = new Dictionary<string, string>
            {
                [Attribute1] = AttributeId1
            };

            var fromStory = new List<AttributeDesignations>
            {
                new AttributeDesignations
                {
                    Id = AttributeId2,
                    Name = Attribute2
                }
            };

            var helper = Mock.Of<IStoryAttributesHelper>(h =>
                h.GetAttributeMappings(It.IsAny<AttributeDesignations>()) == Task.FromResult(fromSql) &&
                h.GetStoryAttributes(It.IsAny<AttributeDesignations>()) == Task.FromResult(fromStory));

            var vm = new AttributeMappingEditorViewModel(helper);

            // Act

            await vm.InitialiseEditor(mapping);

            // Assert

            Assert.AreEqual(1, vm.AttributeMappings.Count);
            Assert.AreEqual(Attribute1, vm.AttributeMappings[0].SourceName);
            Assert.IsTrue(vm.AttributeMappings[0].IsBrokenMapping);
        }

        [Test]
        public void MappingCanBeExtracted()
        {
            // Arrange

            var helper = Mock.Of<IStoryAttributesHelper>();
            var vm = new AttributeMappingEditorViewModel(helper)
            {
                AttributeMappings = new List<AttributeMapping>
                {
                    new AttributeMapping
                    {
                        SourceName = Attribute1,
                        Target = new AttributeDesignations
                        {
                            Id = AttributeId1,
                            Name = Attribute1
                        }
                    },
                    
                    new AttributeMapping
                    {
                        SourceName = Attribute2,
                        Target = new AttributeDesignations
                        {
                            Id = AttributeId3,
                            Name = Attribute3
                        }
                    }
                }
            };

            // Act

            var mapping = vm.ExtractMapping();

            // Assert

            Assert.AreEqual(AttributeId1, mapping[Attribute1]);
            Assert.AreEqual(AttributeId3, mapping[Attribute2]);
        }

        [Test]
        public void StoryAttributesAndMappingsCanBeCleared()
        {
            // Arrange

            var helper = Mock.Of<IStoryAttributesHelper>();
            var vm = new AttributeMappingEditorViewModel(helper)
            {
                AttributeMappings = new List<AttributeMapping>
                {
                    new AttributeMapping()
                },
                
                StoryAttributes = new List<AttributeDesignations>
                {
                    new AttributeDesignations()
                }
            };

            // Act

            vm.Clear();

            // Assert

            Assert.AreEqual(0, vm.AttributeMappings.Count);
            Assert.AreEqual(0, vm.StoryAttributes.Count);
        }
    }
}
