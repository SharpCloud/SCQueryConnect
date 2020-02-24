using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Services;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;
using PanelType = SC.API.ComInterop.Models.Panel.PanelType;

namespace SCQueryConnect.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        private const int FolderTabIndex = 3;
        private const int UpdateStoryTabIndex = 2;

        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IIOService _ioService;
        private readonly IMessageService _messageService;
        private readonly IPasswordStorage _passwordStorage;
        private readonly IProxyViewModel _proxyViewModel;
        private readonly ISaveFileDialogService _saveFileDialogService;

        private PasswordSecurity _publishPasswordSecurity;
        private PublishArchitecture _publishArchitecture;
        private bool _isItemQueryOk = true;
        private bool _isRelationshipQueryOk = true;
        private bool _isPanelsQueryOk = true;
        private bool _isResourceUrlsQueryOk = true;
        private ObservableCollection<QueryData> _connections;
        private QueryData _selectedQueryData;
        private int _lastSelectedConnectionIndex;
        private int _lastSelectedFolderIndex = FolderTabIndex;
        private int _selectedQueryTabIndex;
        private int _selectedTabIndex;
        private string _publishTabHeader;
        private string _updateSubtext;
        private string _updateText;
        private string _url;
        private string _username;

        public PasswordSecurity PublishPasswordSecurity
        {
            get => _publishPasswordSecurity;

            set
            {
                if (_publishPasswordSecurity != value)
                {
                    _publishPasswordSecurity = value;
                    OnPropertyChanged();
                }
            }
        }

        public PublishArchitecture PublishArchitecture
        {
            get => _publishArchitecture;

            set
            {
                if (_publishArchitecture != value)
                {
                    _publishArchitecture = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsItemQueryOk
        {
            get => _isItemQueryOk;

            set
            {
                if (_isItemQueryOk != value)
                {
                    _isItemQueryOk = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRelationshipQueryOk
        {
            get => _isRelationshipQueryOk;

            set
            {
                if (_isRelationshipQueryOk != value)
                {
                    _isRelationshipQueryOk = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPanelsQueryOk
        {
            get => _isPanelsQueryOk;

            set
            {
                if (_isPanelsQueryOk != value)
                {
                    _isPanelsQueryOk = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsResourceUrlsQueryOk
        {
            get => _isResourceUrlsQueryOk;

            set
            {
                if (_isResourceUrlsQueryOk != value)
                {
                    _isResourceUrlsQueryOk = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<QueryData> Connections
        {
            get => _connections;

            set
            {
                if (_connections != value)
                {
                    _connections = value;
                    OnPropertyChanged(nameof(Connections));

                    QueryRootNode.Connections = _connections;
                }
            }
        }

        public QueryData QueryRootNode { get; }

        public QueryData SelectedQueryData
        {
            get => _selectedQueryData;

            set
            {
                if (_selectedQueryData != value)
                {
                    _selectedQueryData = value;
                    OnPropertyChanged();

                    if (_selectedQueryData != null)
                    {
                        if (_selectedQueryData.IsFolder)
                        {
                            PublishTabHeader = "2. Publish";
                            SelectedTabIndex = _lastSelectedFolderIndex;
                        }
                        else
                        {
                            PublishTabHeader = "4. Publish";
                            SelectedTabIndex = _lastSelectedConnectionIndex;
                        }
                    }
                }
            }
        }

        public int SelectedQueryTabIndex
        {
            get => _selectedQueryTabIndex;

            set
            {
                if (_selectedQueryTabIndex != value)
                {
                    _selectedQueryTabIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;

            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();

                    if (SelectedQueryData == null)
                    {
                        return;
                    }

                    if (SelectedQueryData.IsFolder)
                    {
                        _lastSelectedFolderIndex = _selectedTabIndex;
                    }
                    else
                    {
                        _lastSelectedConnectionIndex = _selectedTabIndex;
                    }
                }
            }
        }

        public string PublishTabHeader
        {
            get => _publishTabHeader;
            
            set
            {
                if (_publishTabHeader != value)
                {
                    _publishTabHeader = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdateSubtext
        {
            get => _updateSubtext;

            set
            {
                if (_updateSubtext != value)
                {
                    _updateSubtext = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdateText
        {
            get => _updateText;

            set
            {
                if (_updateText != value)
                {
                    _updateText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Url
        {
            get => _url;

            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get => _username;

            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel(
            IEncryptionHelper encryptionHelper,
            IIOService ioService,
            IMessageService messageService,
            IPasswordStorage passwordStorage,
            IProxyViewModel proxyViewModel,
            ISaveFileDialogService saveFileDialogService)
        {
            _encryptionHelper = encryptionHelper;
            _ioService = ioService;
            _messageService = messageService;
            _passwordStorage = passwordStorage;
            _proxyViewModel = proxyViewModel;
            _saveFileDialogService = saveFileDialogService;

            QueryRootNode = CreateNewFolder(QueryData.RootId);
            QueryRootNode.Id = QueryData.RootId;
        }

        private QueryData FindQueryData(Func<QueryData, bool> predicate)
            => FindQueryData(QueryRootNode, predicate);

        private static QueryData FindQueryData(QueryData data, Func<QueryData, bool> predicate)
        {
            var result = predicate(data);

            if (result)
            {
                return data;
            }

            QueryData toReturn = null;

            if (data.Connections != null)
            {
                foreach (var c in data.Connections)
                {
                    toReturn = FindQueryData(c, predicate);

                    if (toReturn != null)
                    {
                        break;
                    }
                }
            }

            return toReturn;
        }

        public QueryData FindParent(QueryData queryData) => FindQueryData(qd =>
            queryData != null &&
            qd.Connections != null &&
            qd.Connections.Any(c => c.Id == queryData.Id));

        public void SelectUpdateTab()
        {
            SelectedTabIndex = SelectedQueryData.IsFolder
                ? FolderTabIndex
                : UpdateStoryTabIndex;
        }

        public void CreateNewConnection(DatabaseType dbType)
        {
            var queryData = new QueryData(dbType);
            AddQueryData(queryData);
        }

        public void CreateNewFolder()
        {
            var queryData = CreateNewFolder("New Folder");
            AddQueryData(queryData);
        }

        public void MoveConnectionDown()
        {
            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData);

            if (index < parent.Connections.Count - 1)
            {
                parent.Connections.Move(index, index + 1);
            }
        }

        public void MoveConnectionUp()
        {
            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData);

            if (index > 0)
            {
                parent.Connections.Move(index, index - 1);
            }
        }

        public void CopyConnection()
        {
            var queryData = new QueryData(SelectedQueryData);
            AddQueryData(queryData);
        }

        public void DeleteConnection()
        {
            if (SelectedQueryData.IsFolder)
            {
                var result = _messageService.Show(
                    "All folder contents will also be deleted. Do you want to proceed?",
                    "WARNING",
                    MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData) - 1;

            var toSelect = index > -1
                ? parent.Connections[index]
                : parent;

            parent.Connections.Remove(SelectedQueryData);

            if (toSelect != QueryRootNode)
            {
                toSelect.IsSelected = true;
            }
        }

        public void LoadApplicationState()
        {
            var migrate =
                _ioService.FileExists(_ioService.V3ConnectionsPath) &&
                !_ioService.FileExists(_ioService.V4ConnectionsPath);

            string filePath;

            if (migrate)
            {
                if (_ioService.FileExists(_ioService.V3ConnectionsBackupPath))
                {
                    _ioService.DeleteFile(_ioService.V3ConnectionsBackupPath);
                }

                _ioService.MoveFile(_ioService.V3ConnectionsPath, _ioService.V3ConnectionsBackupPath);
                filePath = _ioService.V3ConnectionsBackupPath;
            }
            else
            {
                filePath = _ioService.V4ConnectionsPath;
            }

            LoadAllConnections(migrate, filePath);
            LoadGlobalSettings(migrate);
        }

        private void LoadGlobalSettings(bool migrate)
        {
            if (migrate)
            {
                SaveHelper.RegDelete("ActiveConnection");
            }

            Url = SaveHelper.RegRead("URL", "https://my.sharpcloud.com");
            Username = SaveHelper.RegRead("Username", "");

            _proxyViewModel.Proxy = SaveHelper.RegRead("Proxy", "");
            _proxyViewModel.ProxyAnonymous = bool.Parse(SaveHelper.RegRead("ProxyAnonymous", "true"));
            _proxyViewModel.ProxyUserName = SaveHelper.RegRead("ProxyUserName", "");

            _proxyViewModel.ProxyPassword = _passwordStorage
                .LoadPassword(PasswordStorage.ProxyPassword);
        }

        private void LoadAllConnections(bool migrate, string filePath)
        {
            IList<QueryData> connections;
            var createExamples = !_ioService.FileExists(filePath);

            if (createExamples)
            {
                connections = new List<QueryData>(new[]
                {
                    new QueryData(DatabaseType.Excel),
                    new QueryData(DatabaseType.Access),
                    new QueryData(DatabaseType.SharePointList),
                    new QueryData(DatabaseType.Sql),
                    new QueryData(DatabaseType.Odbc),
                    new QueryData(DatabaseType.Ado),
                    new QueryData(DatabaseType.SharpCloudExcel)
                });
            }
            else // load previous settings
            {
                IList<QueryData> encrypted;

                if (migrate)
                {
                    encrypted = JsonConvert.DeserializeObject<IList<QueryData>>(_ioService.ReadAllTextFromFile(filePath));
                    SaveHelper.RegDelete("ActiveTab");
                }
                else
                {
                    var saveData = JsonConvert.DeserializeObject<SaveData>(_ioService.ReadAllTextFromFile(filePath));
                    encrypted = saveData.Connections;
                    _lastSelectedConnectionIndex = saveData.LastSelectedConnectionIndex;
                    _lastSelectedFolderIndex = saveData.LastSelectedFolderIndex;
                    SelectedQueryTabIndex = saveData.SelectedQueryTabIndex;
                    SelectedTabIndex = saveData.SelectedTabIndex;
                }

                var filtered = encrypted?.Where(qd => qd != null);
                var decrypted = CreateDecryptedPasswordConnections(filtered);
                connections = new List<QueryData>(decrypted);
            }

            var rootLevelConnections = connections.Where(c => !c.IsFolder).ToArray();
            var createDefaultFolder = (createExamples || migrate) && rootLevelConnections.Any();

            if (createDefaultFolder)
            {
                var defaultFolder = CreateNewFolder("My Connections");
                defaultFolder.IsExpanded = true;
                defaultFolder.IsSelected = true;
                defaultFolder.Connections = new ObservableCollection<QueryData>(
                    rootLevelConnections);

                var otherConnections = connections.Where(c => c.Connections != null);
                var allConnections = new List<QueryData>
                {
                    defaultFolder
                }.Concat(otherConnections);

                Connections = new ObservableCollection<QueryData>(allConnections);
            }
            else
            {
                Connections = new ObservableCollection<QueryData>(connections);
            }
        }

        public void SaveApplicationState()
        {
            SaveHelper.RegWrite("URL", Url);
            SaveHelper.RegWrite("Username", Username);

            SaveHelper.RegWrite("Proxy", _proxyViewModel.Proxy);
            SaveHelper.RegWrite("ProxyAnonymous", _proxyViewModel.ProxyAnonymous);
            SaveHelper.RegWrite("ProxyUserName", _proxyViewModel.ProxyUserName);

            _passwordStorage.SavePassword(
                PasswordStorage.ProxyPassword,
                _proxyViewModel.ProxyPassword);

            SaveConnections(
                _ioService.OutputRoot,
                IOService.ConnectionsFileV4,
                QueryRootNode,
                true);
        }

        private void SaveConnections(
            string saveFolderPath,
            string filename,
            QueryData root,
            bool connectionsOnly)
        {
            var connections = root.IsFolder && connectionsOnly
                ? new List<QueryData>(root.Connections)
                : new List<QueryData>(new[] { root });

            _ioService.CreateDirectory(saveFolderPath);
            var encrypted = CreateEncryptedPasswordConnections(connections);

            var toSave = new SaveData
            {
                Connections = encrypted,
                LastSelectedConnectionIndex = _lastSelectedConnectionIndex,
                LastSelectedFolderIndex = _lastSelectedFolderIndex,
                SelectedQueryTabIndex = SelectedQueryTabIndex,
                SelectedTabIndex = SelectedTabIndex
            };

            var json = JsonConvert.SerializeObject(toSave);
            var path = Path.Combine(saveFolderPath, filename);
            _ioService.WriteAllTextToFile(path, json);
        }

        public void ExportQueryDataClick(QueryData queryData)
        {
            var fileName = _saveFileDialogService.PromptForExportPath(queryData?.Name);
            
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var directory = Path.GetDirectoryName(fileName);
            var file = Path.GetFileName(fileName);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                SaveConnections(directory, file, queryData, false);
                Process.Start(directory);
            }
        }

        public void ValidatePanelData(QueryData queryData)
        {
            var index = queryData.QueryResultsPanels.Columns.IndexOf("PanelType");
            var invalid = new List<string>();

            foreach (DataRow row in queryData.QueryResultsPanels.Rows)
            {
                var data = (string) row[index];
                var success = Enum.TryParse(data, true, out PanelType _);

                if (!success)
                {
                    invalid.Add(data);
                }
            }

            if (invalid.Count > 0)
            {
                var invalidStr = string.Join(", ", invalid);
                var msg = $"Unrecognized panel types: '{invalidStr}'. Valid values are [{PanelTypeHelper.ValidTypes}]";
                _messageService.Show(msg);
            }
        }

        public void ImportConnections(string filePath)
        {
            if (!_ioService.FileExists(filePath))
            {
                return;
            }
            
            var result = _messageService.Show(
                "Do you want to replace your connections? Select No to merge connection sets, or Cancel to abort",
                "Import Connections",
                MessageBoxButton.YesNoCancel);
            
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            try
            {
                var saveData = JsonConvert.DeserializeObject<SaveData>(
                    _ioService.ReadAllTextFromFile(filePath));

                var encrypted = saveData.Connections;
                var filtered = encrypted?.Where(qd => qd != null);
                var decrypted = CreateDecryptedPasswordConnections(filtered);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Connections = new ObservableCollection<QueryData>(decrypted);
                        break;
                    case MessageBoxResult.No:
                        Connections = new ObservableCollection<QueryData>(
                            Connections.Concat(decrypted));
                        break;
                }
            }
            catch (Exception e)
            {
                var msg = $"Could not import {filePath}" +
                          $" {Environment.NewLine}" +
                          $" {Environment.NewLine}" +
                          e.Message;

                _messageService.Show(msg);
            }
        }

        private IList<QueryData> CreateDecryptedPasswordConnections(IEnumerable<QueryData> connections)
        {
            var newConnections = new List<QueryData>();

            foreach (var connection in connections)
            {
                var json = JsonConvert.SerializeObject(connection);
                var copy = JsonConvert.DeserializeObject<QueryData>(json);

                var hasPassword = !string.IsNullOrWhiteSpace(copy.SourceStoryPassword);
                if (!hasPassword && !string.IsNullOrWhiteSpace(copy.SourceStoryPasswordDpapi))
                {
                    copy.SourceStoryPassword = _encryptionHelper.TextEncoding.GetString(
                        _encryptionHelper.Decrypt(
                            copy.SourceStoryPasswordDpapi,
                            copy.SourceStoryPasswordEntropy,
                            DataProtectionScope.CurrentUser));
                }

                newConnections.Add(copy);
            }

            return newConnections;
        }

        private List<QueryData> CreateEncryptedPasswordConnections(IList<QueryData> connections)
        {
            var newConnections = new List<QueryData>();

            foreach (var connection in connections)
            {
                var json = JsonConvert.SerializeObject(connection);
                var copy = JsonConvert.DeserializeObject<QueryData>(json);
                var hasPassword = !string.IsNullOrWhiteSpace(copy.SourceStoryPassword);

                if (hasPassword)
                {
                    copy.SourceStoryPasswordDpapi = Convert.ToBase64String(
                        _encryptionHelper.Encrypt(
                            _encryptionHelper.TextEncoding.GetBytes(copy.SourceStoryPassword),
                            out var entropy,
                            DataProtectionScope.CurrentUser));

                    copy.SourceStoryPasswordEntropy = Convert.ToBase64String(entropy);

                    copy.SourceStoryPassword = null;
                }
                else
                {
                    copy.SourceStoryPasswordDpapi = null;
                }

                newConnections.Add(copy);
            }

            return newConnections;
        }

        private void AddQueryData(QueryData queryData)
        {
            Connections.Add(queryData);
            queryData.IsSelected = true;
            
            SelectedTabIndex = queryData.IsFolder // go back to the first tab
                ? FolderTabIndex
                : 0;
        }

        private static QueryData CreateNewFolder(string name)
        {
            return new QueryData
            {
                Name = name,
                Connections = new ObservableCollection<QueryData>(),
                ConnectionType = DatabaseType.Folder
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
