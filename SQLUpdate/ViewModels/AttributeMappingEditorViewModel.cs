using System;
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
        private readonly IMainViewModel _mainViewModel;
        private readonly IMessageService _messageService;
        private readonly IStoryAttributesHelper _storyAttributesHelper;

        private readonly AttributeDesignations _unassigned = new AttributeDesignations
        {
            Id = string.Empty,
            Name = "- Unassigned -"
        };

        private bool _isInitialised;
        private List<AttributeDesignations> _storyAttributes;
        private List<AttributeMapping> _attributeMappings;

        public bool IsInitialised
        {
            get => _isInitialised;

            set
            {
                if (_isInitialised != value)
                {
                    _isInitialised = value;
                    OnPropertyChanged();
                }
            }
        }

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
            IMainViewModel mainViewModel,
            IMessageService messageService,
            IStoryAttributesHelper storyAttributesHelper)
        {
            _mainViewModel = mainViewModel;
            _messageService = messageService;
            _storyAttributesHelper = storyAttributesHelper;
        }

        public async Task InitialiseEditor(IDictionary<string, string> existingMapping)
        {
            _mainViewModel.UpdateText = "Loading attributes...";

            var attributesTask = _storyAttributesHelper.GetStoryAttributes(_unassigned);
            var mappingsTask = _storyAttributesHelper.GetAttributeMappings(_unassigned);
            
            try
            {
                await Task.WhenAll(attributesTask, mappingsTask);
            }
            catch (Exception e)
            {
                _messageService.Show(e.Message);
            }

            _mainViewModel.UpdateText = string.Empty;

            if (attributesTask.Status != TaskStatus.RanToCompletion ||
                attributesTask.Result == null ||
                mappingsTask.Status != TaskStatus.RanToCompletion ||
                mappingsTask.Result == null)
            {
                OnInitialisationError();
                _messageService.Show("Closing mapping editor: could not generate attributes mappings");
                return;
            }

            if (existingMapping != null)
            {
                foreach (var mapping in mappingsTask.Result)
                {
                    var attribute = attributesTask.Result.SingleOrDefault(a =>
                        existingMapping.ContainsKey(mapping.SourceName) &&
                        a.Id == existingMapping[mapping.SourceName]);

                    mapping.Target = attribute ?? _unassigned;

                    mapping.IsBrokenMapping =
                        mapping.Target == _unassigned ||
                        !existingMapping.ContainsKey(mapping.SourceName);
                }
            }

            StoryAttributes = attributesTask.Result;
            AttributeMappings = mappingsTask.Result;
            IsInitialised = StoryAttributes != null && AttributeMappings != null;
        }

        public Dictionary<string, string> ExtractMapping()
        {
            var mapping = AttributeMappings.ToDictionary(m => m.SourceName, m => m.Target.Id);
            return mapping;
        }

        public void Clear()
        {
            AttributeMappings = new List<AttributeMapping>();
            StoryAttributes = new List<AttributeDesignations>();
        }

        public event EventHandler InitialisationError;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnInitialisationError()
        {
            InitialisationError?.Invoke(this, EventArgs.Empty);
        }
    }
}
