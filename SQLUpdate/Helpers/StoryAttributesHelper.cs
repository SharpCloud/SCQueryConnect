using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.Services;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Helpers
{
    public class StoryAttributesHelper : IStoryAttributesHelper
    {
        private readonly IMainViewModel _mainViewModel;
        private readonly IMessageService _messageService;
        private readonly IPasswordStorage _passwordStorage;
        private readonly IProxyViewModel _proxyViewModel;
        private readonly IQueryConnectHelper _queryConnectHelper;
        private readonly ISharpCloudApiFactory _scApiFactory;

        public StoryAttributesHelper(
            IMainViewModel mainViewModel,
            IMessageService messageService,
            IPasswordStorage passwordStorage,
            IProxyViewModel proxyViewModel,
            IQueryConnectHelper queryConnectHelper,
            ISharpCloudApiFactory scApiFactory)
        {
            _mainViewModel = mainViewModel;
            _messageService = messageService;
            _passwordStorage = passwordStorage;
            _proxyViewModel = proxyViewModel;
            _queryConnectHelper = queryConnectHelper;
            _scApiFactory = scApiFactory;
        }

        public Task<List<AttributeDesignations>> GetStoryAttributes(AttributeDesignations unassigned)
        {
            var storyTask = Task.Run(() =>
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

                var story = sc.LoadStory(_mainViewModel.SelectedQueryData.StoryId);

                var attributes = story.Attributes
                    .Where(a => a.IsUserDefined)
                    .Select(a =>
                        new AttributeDesignations
                        {
                            Id = a.Id,
                            Name = a.Name
                        })
                    .ToList();

                var allAttributes = attributes.Prepend(unassigned).ToList();
                return allAttributes;
            });

            return storyTask;
        }

        public async Task<List<AttributeMapping>> GetAttributeMappings(AttributeDesignations unassigned)
        {
            var sqlResultsTask = await _mainViewModel.PreviewSql(
                _mainViewModel.SelectedQueryData,
                _mainViewModel.SelectedQueryData.QueryString,
                QueryEntityType.Items);

            var mappingList = sqlResultsTask.Columns
                .Cast<DataColumn>()
                .Select(c => c.Caption)
                .Where(_queryConnectHelper.IsCustomAttribute)
                .Select(a => new AttributeMapping
                {
                    SourceName = a,
                    Target = unassigned
                });

            return mappingList.OrderBy(a => a.SourceName).ToList();
        }
    }
}
