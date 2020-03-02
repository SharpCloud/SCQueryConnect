﻿using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Common.Services;
using SCQueryConnect.Controls;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Logging;
using SCQueryConnect.Models;
using SCQueryConnect.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PanelType = SC.API.ComInterop.Models.Panel.PanelType;

namespace SCQueryConnect.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        public const int FolderTabIndex = 3;
        public const int QueriesTabIndex = 1;
        public const int UpdateStoryTabIndex = 2;

        private readonly int _maxRowCount;
        private readonly IBatchPublishHelper _batchPublishHelper;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDictionary<QueryEntityType, IDataChecker> _dataCheckers;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IIOService _ioService;
        private readonly MultiDestinationLogger _logger;
        private readonly IMessageService _messageService;
        private readonly IPasswordStorage _passwordStorage;
        private readonly IProxyViewModel _proxyViewModel;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly ISaveFileDialogService _saveFileDialogService;

        private CancellationTokenSource _cancellationTokenSource;
        private string _lastUsedSharpCloudConnection;

        private PasswordSecurity _publishPasswordSecurity;
        private PublishArchitecture _publishArchitecture;
        private bool _canCancelUpdate = true;
        private ObservableCollection<QueryData> _connections;
        private QueryData _selectedQueryData;
        private int _lastSelectedConnectionIndex;
        private int _lastSelectedFolderIndex = FolderTabIndex;
        private int _selectedQueryTabIndex;
        private TabItem _selectedQueryTabItem;
        private int _selectedTabIndex;
        private string _publishTabHeader;
        private string _updateSubtext;
        private string _updateText;
        private string _url;
        private string _username;

        public bool IsUpdateRunning =>
            !string.IsNullOrWhiteSpace(UpdateText) ||
            !string.IsNullOrWhiteSpace(UpdateSubtext);

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

        public bool CanCancelUpdate
        {
            get => _canCancelUpdate;

            set
            {
                if (_canCancelUpdate != value)
                {
                    _canCancelUpdate = value;
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

        public TabItem SelectedQueryTabItem
        {
            get => _selectedQueryTabItem;

            set
            {
                if (_selectedQueryTabItem != value)
                {
                    _selectedQueryTabItem = value;
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
            IBatchPublishHelper batchPublishHelper,
            IEnumerable<IDataChecker> dataCheckers,
            IDbConnectionFactory dbConnectionFactory,
            IEncryptionHelper encryptionHelper,
            IIOService ioService,
            ILog logger,
            IMessageService messageService,
            IPasswordStorage passwordStorage,
            IProxyViewModel proxyViewModel,
            IQueryConnectHelper qcHelper,
            ISaveFileDialogService saveFileDialogService)
        {
            try
            {
                _maxRowCount = int.Parse(ConfigurationManager.AppSettings["MaxRowCount"]);
            }
            catch (Exception)
            {
                _maxRowCount = 1000;
            }

            _batchPublishHelper = batchPublishHelper;
            _dbConnectionFactory = dbConnectionFactory;
            _encryptionHelper = encryptionHelper;
            _ioService = ioService;
            _logger = (MultiDestinationLogger) logger;
            _messageService = messageService;
            _passwordStorage = passwordStorage;
            _proxyViewModel = proxyViewModel;
            _qcHelper = qcHelper;
            _saveFileDialogService = saveFileDialogService;

            _dataCheckers = dataCheckers.ToDictionary(c => c.TargetEntity, c => c);

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

        private void SelectUpdateTab()
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
            try
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
            catch (JsonReaderException ex)
            {
                var msg = $"Error occurred reading saved connections at {_ioService.V4ConnectionsPath}" +
                          Environment.NewLine +
                          Environment.NewLine +
                          "Click OK to exit QueryConnect" +
                          Environment.NewLine +
                          Environment.NewLine +
                          ex.Message;

                _messageService.Show(msg);
                throw;
            }
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
            if (queryData.QueryResultsPanels == null)
            {
                return;
            }

            var index = queryData.QueryResultsPanels.Columns.IndexOf("PanelType");
            var invalid = new List<string>();

            foreach (DataRow row in queryData.QueryResultsPanels.Rows)
            {
                var data = row[index] as string;
                var success = Enum.TryParse(data, true, out PanelType _);

                if (!success)
                {
                    invalid.Add(data);
                }
            }

            if (invalid.Count > 0)
            {
                var unrecognised = invalid.Where(s => !string.IsNullOrWhiteSpace(s));
                var unrecognisedStr = string.Join(", ", unrecognised);

                var unrecognisedMsg = string.IsNullOrWhiteSpace(unrecognisedStr)
                    ? string.Empty
                    : $"Unrecognised panel types: '{unrecognisedStr}'. Valid values are [{PanelTypeHelper.ValidTypes}]";

                var unspecified = invalid.Count(string.IsNullOrWhiteSpace);

                var unspecifiedMsg = unspecified == 0
                    ? string.Empty
                    : $"Data contains {unspecified} unspecified panel type(s)";

                var messages = new[] {unrecognisedMsg, unspecifiedMsg}
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                var msg = string.Join(". ", messages);
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

        private SharpCloudConfiguration GetApiConfiguration()
        {
            return new SharpCloudConfiguration
            {
                Username = Username,
                Password = _passwordStorage.LoadPassword(PasswordStorage.Password),
                Url = Url,
                ProxyUrl = _proxyViewModel.Proxy,
                UseDefaultProxyCredentials = _proxyViewModel.ProxyAnonymous,
                ProxyUserName = _proxyViewModel.ProxyUserName,
                ProxyPassword = _proxyViewModel.ProxyPassword
            };
        }

        private IDbConnection GetDb(QueryData queryData)
        {
            return _dbConnectionFactory.GetDb(
                queryData.FormattedConnectionString,
                queryData.ConnectionType);
        }

        public async Task PreviewSql()
        {
            if (!IsUpdateRunning &&
                SelectedTabIndex == QueriesTabIndex &&
                SelectedQueryTabItem.Content is QueryEditor editor)
            {
                Expression<Func<QueryData, DataTable>> resultsSelector;

                switch (editor.TargetEntity)
                {
                    case QueryEntityType.Items:
                        resultsSelector = d => d.QueryResults;
                        UpdateSubtext = "Running Items Query";
                        break;

                    case QueryEntityType.Relationships:
                        resultsSelector = d => d.QueryResultsRels;
                        UpdateSubtext = "Running Relationships Query";
                        break;

                    case QueryEntityType.ResourceUrls:
                        resultsSelector = d => d.QueryResultsResourceUrls;
                        UpdateSubtext = "Running Resource URLs Query";
                        break;

                    case QueryEntityType.Panels:
                        resultsSelector = d => d.QueryResultsPanels;
                        UpdateSubtext = "Running Panels Query";
                        break;

                    default:
                        _messageService.Show($"Unknown query type: {editor.TargetEntity}");
                        UpdateSubtext = string.Empty;
                        return;
                }

                UpdateText = "Generating SQL Preview...";
                var queryData = SelectedQueryData;
                DataTable dataTable;

                dataTable = await PreviewSql(
                    queryData,
                    editor.SelectedQueryString,
                    editor.TargetEntity);

                var prop = (PropertyInfo)((MemberExpression)resultsSelector.Body).Member;
                prop.SetValue(queryData, dataTable, null);

                ValidatePanelData(queryData);
            }
        }

        public async Task<DataTable> PreviewSql(
            QueryData queryData,
            string query,
            QueryEntityType targetEntity)
        {
            CanCancelUpdate = false;
            DataTable dataTable;

            await InitialiseSharpCloudDataIfNeeded(queryData);

            using (var connection = GetDb(queryData))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    using (var reader = command.ExecuteReader())
                    {
                        await _dataCheckers[targetEntity].CheckData(reader, queryData);

                        dataTable = new DataTable();
                        dataTable.Load(reader);
                    }
                }
            }

            var regex = new Regex(Regex.Escape("#"));
            for (var c = 0; c < dataTable.Columns.Count; c++)
            {
                var col = dataTable.Columns[c];
                if (col.Caption.ToLower().StartsWith("tags#"))
                {
                    col.Caption = regex.Replace(col.Caption, ".", 1);
                }
            }

            SetLastUsedSharpCloudConnection(queryData);
            return dataTable;
        }

        public async Task RunQueryData(QueryData queryData)
        {
            if (IsUpdateRunning)
            {
                return;
            }

            try
            {
                CanCancelUpdate = true;
                _cancellationTokenSource = new CancellationTokenSource();
                await RunQueryData(queryData, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarning("Update cancelled");
                _logger.ClearDestinations();

                UpdateText = string.Empty;
                UpdateSubtext = string.Empty;
            }
        }

        private async Task RunQueryData(QueryData queryData, CancellationToken ct)
        {
            if (!ValidateCredentials() ||
                !ValidateAllStoryIds(queryData))
            {
                return;
            }

            queryData.IsSelected = true;
            SelectUpdateTab();

            await Task.Delay(100);

            await _logger.Clear();
            SaveApplicationState();

            await RunAllQueryData(queryData, ct);

            queryData.LastRunDateTime = DateTime.Now;
            SaveApplicationState();

            UpdateText = string.Empty;
            UpdateSubtext = string.Empty;
        }

        private async Task RunAllQueryData(QueryData queryData, CancellationToken ct)
        {
            var destination = new QueryDataLoggingDestination(queryData);
            await destination.Clear();

            _logger.PushDestination(destination);

            if (queryData.IsFolder)
            {
                foreach (var data in queryData.Connections)
                {
                    await RunAllQueryData(data, ct);
                    ct.ThrowIfCancellationRequested();
                }
            }
            else
            {
                UpdateText = "Updating...";
                UpdateSubtext = $"Running '{queryData.Name}'";

                var message = _batchPublishHelper.GetBatchRunStartMessage(queryData.Name);
                await _logger.Log(message);

                await UpdateSharpCloud(queryData, ct);
            }

            _logger.PopDestination();
        }

        private async Task UpdateSharpCloud(QueryData queryData, CancellationToken ct)
        {
            var config = GetApiConfiguration();

            var settings = new UpdateSettings
            {
                TargetStoryId = queryData.StoryId,
                QueryString = queryData.QueryString,
                QueryStringPanels = queryData.QueryStringPanels,
                QueryStringRels = queryData.QueryStringRels,
                QueryStringResourceUrls = queryData.QueryStringResourceUrls,
                ConnectionString = queryData.FormattedConnectionString,
                DBType = queryData.ConnectionType,
                MaxRowCount = _maxRowCount,
                UnpublishItems = queryData.UnpublishItems,
                BuildRelationships = queryData.BuildRelationships
            };

            await _qcHelper.UpdateSharpCloud(config, settings, ct);
        }

        public async Task CancelStoryUpdate()
        {
            await _logger.LogWarning("Update cancellation initiated by user...");

            UpdateText = "Update Cancelled...";
            UpdateSubtext = "Finishing current task...";

            _cancellationTokenSource.Cancel();
        }

        public void PublishBatchFolder()
        {
            if (!ValidateCredentials())
            {
                return;
            }

            var settings = new PublishSettings
            {
                Data = SelectedQueryData,
                ProxyViewModel = _proxyViewModel,
                Username = Username,
                SharpCloudUrl = Url,
                PasswordSecurity = PublishPasswordSecurity,
                PublishArchitecture = PublishArchitecture
            };

            _batchPublishHelper.PublishBatchFolder(settings);
        }

        public async Task TestConnection(QueryData queryData)
        {
            if (queryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                try
                {
                    await _qcHelper.InitialiseDatabase(
                        GetApiConfiguration(),
                        queryData.FormattedConnectionString,
                        queryData.ConnectionType);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not connect to SharpCloud! {ex.Message}");
                    return;
                }
            }

            try
            {
                using (IDbConnection connection = GetDb(queryData))
                {
                    connection.Open();
                    _messageService.Show("Hooray! It looks like it's worked!");
                    SaveApplicationState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not connect to database! " + ex.Message);
            }
        }

        private bool ValidateCredentials()
        {
            if (string.IsNullOrEmpty(Url))
            {
                MessageBox.Show("Please enter a valid URL");
                return false;
            }

            if (string.IsNullOrEmpty(Username))
            {
                MessageBox.Show("Please enter your SharpCloud username");
                return false;
            }

            if (string.IsNullOrEmpty(_passwordStorage.LoadPassword(PasswordStorage.Password)))
            {
                MessageBox.Show("Please enter your password");
                return false;
            }

            return true;
        }

        private bool ValidateAllStoryIds(QueryData qd)
        {
            bool valid;

            if (qd.IsFolder)
            {
                valid = qd.Connections.Aggregate(
                    true,
                    (isValid, data) => isValid && ValidateAllStoryIds(data));

                return valid;
            }
            else
            {
                valid = ValidateStoryId(qd);
            }

            return valid;
        }

        private bool ValidateStoryId(QueryData queryData)
        {
            if (string.IsNullOrEmpty(queryData.StoryId))
            {
                MessageBox.Show($"Please enter a story ID for '{queryData.Name}'");
                return false;
            }

            if (!Guid.TryParse(queryData.StoryId, out _))
            {
                MessageBox.Show($"Story ID for '{queryData.Name}' must be a GUID");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if updating the database is necessary, e.g. an update is only necessary
        /// when running queries against data when the query is run for the first time;
        /// subsequent runs should be able to use local data to speed up the process.
        /// </summary>
        private async Task InitialiseSharpCloudDataIfNeeded(QueryData queryData)
        {
            if (queryData.ConnectionType == DatabaseType.SharpCloudExcel &&
                queryData.FormattedConnectionString != _lastUsedSharpCloudConnection)
            {
                await _qcHelper.InitialiseDatabase(
                    GetApiConfiguration(),
                    queryData.FormattedConnectionString,
                    queryData.ConnectionType);
            }
        }

        private void SetLastUsedSharpCloudConnection(QueryData queryData)
        {
            if (queryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                _lastUsedSharpCloudConnection = queryData.FormattedConnectionString;
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
