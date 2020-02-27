using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SCQueryConnect.ViewModels
{
    public class AttributeMappingEditorViewModel : IAttributeMappingEditorViewModel
    {
        private readonly IStoryAttributesHelper _storyAttributesHelper;

        private readonly AttributeDesignations _unassigned = new AttributeDesignations
        {
            Id = string.Empty,
            Name = "- Unassigned -"
        };

        private List<AttributeDesignations> _storyAttributes;
        private List<AttributeMapping> _attributeMappings;

        public List<AttributeDesignations> StoryAttributes
        {
            get => _storyAttributes;
            
            set
            {
                if (_storyAttributes != value)
                {
                    _storyAttributes = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<AttributeMapping> AttributeMappings
        {
            get => _attributeMappings;

            set
            {
                if (_attributeMappings != value)
                {
                    _attributeMappings = value;
                    OnPropertyChanged();
                }
            }
        }

        public AttributeMappingEditorViewModel(
            IStoryAttributesHelper storyAttributesHelper)
        {
            _storyAttributesHelper = storyAttributesHelper;
        }

        public async Task InitialiseEditor(IDictionary<string, string> existingMapping)
        {
            var attributesTask = _storyAttributesHelper.GetStoryAttributes(_unassigned);
            var mappingsTask = _storyAttributesHelper.GetAttributeMappings(_unassigned);
            await Task.WhenAll(attributesTask, mappingsTask);

            var storyAttributes = attributesTask.Result;
            var generatedMappings = mappingsTask.Result;

            if (existingMapping != null)
            {
                foreach (var mapping in generatedMappings)
                {
                    var attribute = storyAttributes.SingleOrDefault(a =>
                        existingMapping.ContainsKey(mapping.SourceName) &&
                        a.Id == existingMapping[mapping.SourceName]);

                    mapping.Target = attribute ?? _unassigned;

                    mapping.IsBrokenMapping =
                        mapping.Target == _unassigned ||
                        !existingMapping.ContainsKey(mapping.SourceName);
                }
            }

            StoryAttributes = storyAttributes;
            AttributeMappings = generatedMappings;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
