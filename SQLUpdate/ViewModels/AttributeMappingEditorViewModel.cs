using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SCQueryConnect.ViewModels
{
    public class AttributeMappingEditorViewModel : IAttributeMappingEditorViewModel
    {
        private readonly IStoryAttributesHelper _storyAttributesHelper;

        

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

        public async Task InitialiseEditor()
        {
            var attributesTask = _storyAttributesHelper.GetStoryAttributes();
            var mappingsTask = _storyAttributesHelper.GetAttributeMappings();
            await Task.WhenAll(attributesTask, mappingsTask);

            StoryAttributes = attributesTask.Result;
            AttributeMappings = mappingsTask.Result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
