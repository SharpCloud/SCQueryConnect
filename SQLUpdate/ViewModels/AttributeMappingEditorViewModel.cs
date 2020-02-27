using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SCQueryConnect.ViewModels
{
    public class AttributeMappingEditorViewModel : IAttributeMappingEditorViewModel
    {
        private readonly IMainViewModel _mainViewModel;
        private readonly IMessageService _messageService;
        private readonly IPasswordStorage _passwordStorage;
        private readonly IProxyViewModel _proxyViewModel;
        private readonly ISharpCloudApiFactory _scApiFactory;

        private readonly AttributeDesignations _unassignedAttribute = new AttributeDesignations
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
            IMainViewModel mainViewModel,
            IMessageService messageService,
            IPasswordStorage passwordStorage,
            IProxyViewModel proxyViewModel,
            ISharpCloudApiFactory scApiFactory)
        {
            _mainViewModel = mainViewModel;
            _messageService = messageService;
            _passwordStorage = passwordStorage;
            _proxyViewModel = proxyViewModel;
            _scApiFactory = scApiFactory;
        }

        public async Task InitialiseEditor()
        {
            var attributesTask = GetStoryAttributes();
            var mappingsTask = GetAttributeMappings();
            await Task.WhenAll(attributesTask, mappingsTask);

            StoryAttributes = attributesTask.Result;
            AttributeMappings = mappingsTask.Result;
        }

        private Task<List<AttributeDesignations>> GetStoryAttributes()
        {
            var sc = _scApiFactory.CreateSharpCloudApi(
                _mainViewModel.Username,
                _passwordStorage.LoadPassword(PasswordStorage.Password),
                _mainViewModel.Url,
                _proxyViewModel.Proxy,
                _proxyViewModel.ProxyAnonymous,
                _proxyViewModel.ProxyUserName,
                _passwordStorage.LoadPassword(PasswordStorage.ProxyPassword));

            if (sc == null)
            {
                _messageService.Show(InvalidCredentialsException.LoginFailed);
                return null;
            }

            var storyTask = Task.Run(() =>
            {
                var story = sc.LoadStory(_mainViewModel.SelectedQueryData.StoryId);

                var attributes = story.Attributes
                    .Select(a =>
                        new AttributeDesignations
                        {
                            Id = a.Id,
                            Name = a.Name
                        })
                    .ToList();

                var allAttributes = attributes.Prepend(_unassignedAttribute).ToList();
                return allAttributes;
            });

            return storyTask;
        }

        private async Task<List<AttributeMapping>> GetAttributeMappings()
        {
            var sqlResultsTask = await _mainViewModel.PreviewSql(
                _mainViewModel.SelectedQueryData,
                _mainViewModel.SelectedQueryData.QueryString,
                QueryEntityType.Items);

            var mappingList = sqlResultsTask.Columns.Cast<DataColumn>().Select(c =>
                new AttributeMapping
                {
                    SourceName = c.Caption,
                    Target = _unassignedAttribute
                });

            return mappingList.OrderBy(a => a.SourceName).ToList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
