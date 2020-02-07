using Microsoft.Win32;
using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Logging;
using SCQueryConnect.Models;
using SCQueryConnect.ViewModels;
using SCQueryConnect.Views;
using SQLUpdate.Views;
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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SCQueryConnect
{
    // some useful links... https://www.microsoft.com/en-us/download/details.aspx?id=13255 for accessing the access db

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string ConnectionsFileV3 = "connections.json";
        private const string ConnectionsFileV3Backup = "connections.json.bak";
        private const string ConnectionsFileV4 = "connections_v4.json";

        private Point _startPoint;

        private readonly QueryData _queryRootNode;

        public string AppName
        {
            get
            {
                return _qcHelper.AppName;
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

                    _queryRootNode.Connections = _connections;
                }
            }
        }

        private QueryData FindQueryData(Func<QueryData, bool> predicate)
            => FindQueryData(_queryRootNode, predicate);

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

        private QueryData FindParent(QueryData queryData) => FindQueryData(qd =>
            queryData != null &&
            qd.Connections != null &&
            qd.Connections.Any(c => c.Id == queryData.Id));

        private string _lastUsedSharpCloudConnection;
        private ProxyViewModel _proxyViewModel;
        private ObservableCollection<QueryData> _connections;
        private readonly int _maxRowCount;
        private readonly IBatchPublishHelper _batchPublishHelper;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IItemDataChecker _itemDataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IExcelWriter _excelWriter;
        private readonly MultiDestinationLogger _logger;
        private readonly IMainViewModel _mainViewModel;
        private readonly IRelationshipsDataChecker _relationshipsChecker;
        private readonly ISharpCloudApiFactory _sharpCloudApiFactory;
        private readonly IPanelsDataChecker _panelsDataChecker;
        private readonly IResourceUrlDataChecker _resourceUrlDataChecker;
        private readonly string _localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
        private readonly RichTextBoxLoggingDestination _storyLoggingDestination;
        private readonly RichTextBoxLoggingDestination _folderLoggingDestination;

        public MainWindow(
            IBatchPublishHelper batchPublishHelper,
            IConnectionStringHelper connectionStringHelper,
            IItemDataChecker itemDataChecker,
            IDbConnectionFactory dbConnectionFactory,
            IEncryptionHelper encryptionHelper,
            IExcelWriter excelWriter,
            IMainViewModel mainViewModel,
            IRelationshipsDataChecker relationshipsDataChecker,
            ISharpCloudApiFactory sharpCloudApiFactory,
            ILog logger,
            IQueryConnectHelper qcHelper,
            IPanelsDataChecker panelsDataChecker,
            IResourceUrlDataChecker resourceUrlDataChecker)
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContext = this;

            try
            {
                _maxRowCount = int.Parse(ConfigurationManager.AppSettings["MaxRowCount"]);
            }
            catch (Exception)
            {
                _maxRowCount = 1000;
            }

            _queryRootNode = CreateNewFolder(QueryData.RootId);
            _queryRootNode.Id = QueryData.RootId;

            _batchPublishHelper = batchPublishHelper;
            _connectionStringHelper = connectionStringHelper;

            _itemDataChecker = itemDataChecker;
            _itemDataChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterr);

            _dbConnectionFactory = dbConnectionFactory;
            _encryptionHelper = encryptionHelper;
            _excelWriter = excelWriter;
            _mainViewModel = mainViewModel;

            _relationshipsChecker = relationshipsDataChecker;
            _relationshipsChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterrRels);

            _sharpCloudApiFactory = sharpCloudApiFactory;

            _panelsDataChecker = panelsDataChecker;
            _panelsDataChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterrPanels);

            _resourceUrlDataChecker = resourceUrlDataChecker;
            _resourceUrlDataChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterrResourceUrls);

            _logger = (MultiDestinationLogger) logger;
            
            _storyLoggingDestination = new RichTextBoxLoggingDestination(StoryUpdateLogOutput);
            _logger.PushLoggingDestination(_storyLoggingDestination);
            
            _folderLoggingDestination  = new RichTextBoxLoggingDestination(FolderUpdateLogOutput);
            _logger.PushLoggingDestination(_folderLoggingDestination);

            _qcHelper = qcHelper;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var pathV3 = Path.Combine(_localPath, ConnectionsFileV3);
            var migrate = File.Exists(pathV3);
            string filePath;

            if (migrate)
            {
                var pathV3Backup = Path.Combine(_localPath, ConnectionsFileV3Backup);

                if (File.Exists(pathV3Backup))
                {
                    File.Delete(pathV3Backup);
                }

                File.Move(pathV3, pathV3Backup);
                filePath = pathV3Backup;
            }
            else
            {
                filePath = Path.Combine(_localPath, ConnectionsFileV4);
            }

            LoadAllConnections(migrate, filePath);
            LoadGlobalSettings(migrate);

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(SelectAllOnTabFocus));

            EventManager.RegisterClassHandler(
                typeof(PasswordBox),
                GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(SelectAllOnTabFocus));

            var splashScreen = new SplashScreen("Images/splash.jpg");
            splashScreen.Show(true);
        }

        private void SelectAllOnTabFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Tab))
            {
                if (sender is TextBox textBox && !textBox.IsReadOnly)
                {
                    textBox.SelectAll();
                }
                else if (sender is PasswordBox passwordBox)
                {
                    passwordBox.SelectAll();
                }
            }
        }

        private void LoadGlobalSettings(bool migrate)
        {
            if (migrate)
            {
                SaveHelper.RegDelete("ActiveConnection");
            }

            Url.Text = SaveHelper.RegRead("URL", "https://my.sharpcloud.com");
            Username.Text = SaveHelper.RegRead("Username", "");

            var regPassword = SaveHelper.RegRead("PasswordDpapi", "");
            var regPasswordEntropy = SaveHelper.RegRead("PasswordDpapiEntropy", null);
            try
            {
                Password.Password = _encryptionHelper.TextEncoding.GetString(
                    _encryptionHelper.Decrypt(
                        regPassword,
                        regPasswordEntropy,
                        DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException ex) when (ex.Message.Contains("The parameter is incorrect"))
            {
                // Fallback method for backwards compatibility
                regPassword = SaveHelper.RegRead("Password", "");

                Password.Password = Encoding.Default.GetString(
                    Convert.FromBase64String(regPassword));

                SaveHelper.RegDelete("Password");
            }

            _proxyViewModel = new ProxyViewModel();
            _proxyViewModel.Proxy = SaveHelper.RegRead("Proxy", "");
            _proxyViewModel.ProxyAnnonymous = bool.Parse(SaveHelper.RegRead("ProxyAnonymous", "true"));
            _proxyViewModel.ProxyUserName = SaveHelper.RegRead("ProxyUserName", "");

            var regProxyPassword = SaveHelper.RegRead("ProxyPasswordDpapi", "");
            var regProxyPasswordEntropy = SaveHelper.RegRead("ProxyPasswordDpapiEntropy", null);
            try
            {
                _proxyViewModel.ProxyPassword = _encryptionHelper.TextEncoding.GetString(
                    _encryptionHelper.Decrypt(
                        regProxyPassword,
                        regProxyPasswordEntropy,
                        DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException ex) when (ex.Message.Contains("The parameter is incorrect"))
            {
                regProxyPassword = SaveHelper.RegRead("ProxyPassword", "");

                // Fallback method for backwards compatibility
                _proxyViewModel.ProxyPassword = Encoding.Default.GetString(
                    Convert.FromBase64String(regProxyPassword));

                SaveHelper.RegDelete("ProxyPassword");
            }

            BrowserTabs.SelectedIndex = (int.Parse(SaveHelper.RegRead("ActiveTab", "0")));
        }

        private void LoadAllConnections(bool migrate, string filePath)
        {
            IList<QueryData> connections;
            var createExamples = !File.Exists(filePath);

            if (createExamples)
            {
                connections = new List<QueryData>(new[]
                {
                    new QueryData(DatabaseType.Excel),
                    new QueryData(DatabaseType.Access),
                    new QueryData(DatabaseType.SharepointList),
                    new QueryData(DatabaseType.SQL),
                    new QueryData(DatabaseType.ODBC),
                    new QueryData(DatabaseType.ADO),
                    new QueryData(DatabaseType.SharpCloudExcel)
                });
            }
            else // load previous settings
            {
                IList<QueryData> encrypted;

                if (migrate)
                {
                    encrypted = JsonConvert.DeserializeObject<IList<QueryData>>(File.ReadAllText(filePath));
                }
                else
                {
                    var saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(filePath));
                    encrypted = saveData.Connections;
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

        private void RewriteDataSourceClick(object sender, RoutedEventArgs e)
        {
            RewriteDataSource(_mainViewModel.SelectedQueryData);
        }

        private void RewriteDataSource(QueryData queryData)
        {
            var filepath = _connectionStringHelper.GetVariable(
                queryData.FormattedConnectionString,
                DatabaseStrings.DataSourceKey);

            try
            {
                _excelWriter.RewriteExcelFile(filepath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not rewrite Excel data source! {ex.Message}");
            }
        }

        private IDbConnection GetDb(QueryData queryData)
        {
            return _dbConnectionFactory.GetDb(
                queryData.FormattedConnectionString,
                queryData.ConnectionType);
        }

        private SharpCloudConfiguration GetApiConfiguration()
        {
            return new SharpCloudConfiguration
            {
                Username = Username.Text,
                Password = Password.Password,
                Url = Url.Text,
                ProxyUrl = _proxyViewModel.Proxy,
                UseDefaultProxyCredentials = _proxyViewModel.ProxyAnnonymous,
                ProxyUserName = _proxyViewModel.ProxyUserName,
                ProxyPassword = _proxyViewModel.ProxyPassword
            };
        }

        private async void TestConnectionClick(object sender, RoutedEventArgs e)
        {
            await TestConnection(_mainViewModel.SelectedQueryData);
        }

        private async Task TestConnection(QueryData queryData)
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
                    MessageBox.Show("Hooray! It looks like it's worked!");
                    SaveSettings();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not connect to database! " + ex.Message);
            }
        }

        private void ReviewConnectionClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ConnectionInfo(_mainViewModel.SelectedQueryData)
            {
                Owner = this
            };
            dlg.ShowDialog();
        }

        private void SetLastUsedSharpCloudConnection(QueryData queryData)
        {
            if (queryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                _lastUsedSharpCloudConnection = queryData.FormattedConnectionString;
            }
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

        private void ProcessRunException(Exception e)
        {
            switch (e)
            {
                case InvalidOperationException ex when ex.Message.Contains(Constants.AccessDBEngineErrorMessage):
                    var msgbox = new DatabaseErrorMessage { Owner = this };
                    msgbox.ShowDialog();
                    break;

                default:
                    MessageBox.Show($"There was an error: {e.Message}");
                    break;
            }
        }

        private async void RunClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                _mainViewModel.SelectedQueryData,
                _mainViewModel.SelectedQueryData.QueryString,
                _itemDataChecker,
                DataGrid,
                d => d.QueryResults);
        }

        private async void RunClickRels(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                _mainViewModel.SelectedQueryData,
                _mainViewModel.SelectedQueryData.QueryStringRels,
                _relationshipsChecker,
                DataGridRels,
                d => d.QueryResultsRels);
        }

        private async void PreviewResourceUrlsClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                _mainViewModel.SelectedQueryData,
                _mainViewModel.SelectedQueryData.QueryStringResourceUrls,
                _resourceUrlDataChecker,
                DataGridResourceUrls,
                d => d.QueryResultsResourceUrls);
        }

        private async void PreviewPanelsClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                _mainViewModel.SelectedQueryData,
                _mainViewModel.SelectedQueryData.QueryStringPanels,
                _panelsDataChecker,
                DataGridPanels,
                d => d.QueryResultsPanels);
        }

        private async Task PreviewSql(
            QueryData queryData,
            string query,
            IDataChecker dataChecker,
            DataGrid dataGrid,
            Expression<Func<QueryData, DataView>> dataViewSelector)
        {
            try
            {
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
                            dataChecker.CheckData(reader);

                            var dt = new DataTable();
                            dt.Load(reader);

                            var regex = new Regex(Regex.Escape("#"));
                            for (var c = 0; c < dt.Columns.Count; c++)
                            {
                                var col = dt.Columns[c];
                                if (col.Caption.ToLower().StartsWith("tags#"))
                                {
                                    col.Caption = regex.Replace(col.Caption, ".", 1);
                                }
                            }

                            dataGrid.ItemsSource = dt.DefaultView;

                            var prop = (PropertyInfo)((MemberExpression)dataViewSelector.Body).Member;
                            prop.SetValue(_mainViewModel.SelectedQueryData, dt.DefaultView, null);

                            for (var col = 0; col < dataGrid.Columns.Count; col++)
                            {
                                dataGrid.Columns[col].Header = dt.Columns[col].Caption;
                            }
                        }
                    }
                }

                SetLastUsedSharpCloudConnection(queryData);
            }
            catch (Exception ex)
            {
                ProcessRunException(ex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                //check if we are on the UI thread if not switch
                if (Dispatcher.CurrentDispatcher.CheckAccess())
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action<string>(OnPropertyChanged), propertyName);
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            SaveHelper.RegWrite("URL", Url.Text);
            SaveHelper.RegWrite("Username", Username.Text);

            SaveHelper.RegWrite("PasswordDpapi", Convert.ToBase64String(
                _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(Password.Password),
                    out var entropy,
                    DataProtectionScope.CurrentUser)));

            SaveHelper.RegWrite("PasswordDpapiEntropy", Convert.ToBase64String(entropy));
            SaveHelper.RegWrite("ActiveTab", BrowserTabs.SelectedIndex.ToString());

            SaveHelper.RegWrite("Proxy", _proxyViewModel.Proxy);
            SaveHelper.RegWrite("ProxyAnonymous", _proxyViewModel.ProxyAnnonymous);
            SaveHelper.RegWrite("ProxyUserName", _proxyViewModel.ProxyUserName);

            SaveHelper.RegWrite("ProxyPasswordDpapi", Convert.ToBase64String(
                _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(_proxyViewModel.ProxyPassword),
                    out var proxyEntropy,
                    DataProtectionScope.CurrentUser)));

            SaveHelper.RegWrite("ProxyPasswordDpapiEntropy", Convert.ToBase64String(proxyEntropy));
            SaveConnections();
        }

        private void SaveConnections()
        {
            Directory.CreateDirectory(_localPath);
            var connections = CreateEncryptedPasswordConnections(_connections);

            var toSave = new SaveData
            {
                Connections = connections
            };

            var json = JsonConvert.SerializeObject(toSave);
            var path = Path.Combine(_localPath, ConnectionsFileV4);
            File.WriteAllText(path, json);
        }

        private List<QueryData> CreateEncryptedPasswordConnections(ObservableCollection<QueryData> connections)
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

        private bool ValidateCredentials()
        {
            if (string.IsNullOrEmpty(Url.Text))
            {
                MessageBox.Show("Please enter a valid URL");
                Url.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(Username.Text))
            {
                MessageBox.Show("Please enter your SharpCloud username");
                Username.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(Password.Password))
            {
                MessageBox.Show("Please enter your password");
                Password.Focus();
                return false;
            }
            return true;
        }

        private bool ValidateStoryId(QueryData queryData)
        {
            if (string.IsNullOrEmpty(queryData.StoryId))
            {
                MessageBox.Show($"Please enter a story ID for '{queryData.Name}'");
                StoryId.Focus();
                return false;
            }
            
            if (!Guid.TryParse(queryData.StoryId, out _))
            {
                MessageBox.Show($"Story ID for '{queryData.Name}' must be a GUID");
                StoryId.Focus();
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

        private async Task UpdateSharpCloud(QueryData queryData)
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

            await _qcHelper.UpdateSharpCloud(config, settings);
        }

        private void ViewExisting(object sender, RoutedEventArgs e)
        {
            var path = _batchPublishHelper.GetFolder(
                _mainViewModel.SelectedQueryData.Name,
                _localPath);

            Process.Start(path);
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

        private void NewConnectionClick(object sender, RoutedEventArgs e)
        {
            var newWnd = new SelectDatabaseType
            {
                Owner = this
            };

            if (newWnd.ShowDialog() == true)
            {
                var queryData = new QueryData(newWnd.SelectedButton);
                AddQueryData(queryData);
            }
        }

        private void NewQueryFolderClick(object sender, RoutedEventArgs e)
        {
            var queryData = CreateNewFolder("New Folder");
            AddQueryData(queryData);
        }

        private void MoveConnectionDown(object sender, RoutedEventArgs e)
        {
            var parent = FindParent(_mainViewModel.SelectedQueryData);
            var index = parent.Connections.IndexOf(_mainViewModel.SelectedQueryData);
            
            if (index < parent.Connections.Count - 1)
            {
                parent.Connections.Move(index, index + 1);
            }
        }

        private void MoveConnectionUp(object sender, RoutedEventArgs e)
        {
            var parent = FindParent(_mainViewModel.SelectedQueryData);
            var index = parent.Connections.IndexOf(_mainViewModel.SelectedQueryData);

            if (index > 0)
            {
                parent.Connections.Move(index, index - 1);
            }
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var queryData = new QueryData(_mainViewModel.SelectedQueryData);
            AddQueryData(queryData);
        }

        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            var parent = FindParent(_mainViewModel.SelectedQueryData);
            var index = parent.Connections.IndexOf(_mainViewModel.SelectedQueryData) - 1;

            var toSelect = index > -1
                ? parent.Connections[index]
                : parent;
            
            parent.Connections.Remove(_mainViewModel.SelectedQueryData);
            
            if (toSelect != _queryRootNode)
            {
                toSelect.IsSelected = true;
            }
        }

        private void AddQueryData(QueryData queryData)
        {
            Connections.Add(queryData);
            queryData.IsSelected = true;
            BrowserTabs.SelectedIndex = 0; // go back to the first tab
        }

        private async void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _mainViewModel.SelectedQueryData = e.NewValue as QueryData;

            if (_mainViewModel.SelectedQueryData == null)
            {
                return;
            }

            if (_mainViewModel.SelectedQueryData.IsFolder)
            {
                await _folderLoggingDestination.SetLogText(
                    _mainViewModel.SelectedQueryData.LogData);
            }
            else
            {
                await _storyLoggingDestination.SetLogText(
                    _mainViewModel.SelectedQueryData.LogData);

                DataGrid.ItemsSource = _mainViewModel.SelectedQueryData.QueryResults;
                DataGridRels.ItemsSource = _mainViewModel.SelectedQueryData.QueryResultsRels;
                DataGridResourceUrls.ItemsSource = _mainViewModel.SelectedQueryData.QueryResultsResourceUrls;
                DataGridPanels.ItemsSource = _mainViewModel.SelectedQueryData.QueryResultsPanels;
            }
        }

        private void BrowseForDataSourceClick(object sender, RoutedEventArgs e)
        {
            var ord = new OpenFileDialog();

            switch (_mainViewModel.SelectedQueryData.ConnectionType)
            {
                case DatabaseType.Access:
                    ord.Filter = "Access Database Files (*.accdb;*.mdb)|*.accdb;*.mdb";
                    break;

                case DatabaseType.Excel:
                case DatabaseType.SharpCloudExcel:
                    ord.Filter = "Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx";
                    break;
            }

            if (ord.ShowDialog() == true)
            {
                _mainViewModel.SelectedQueryData.FileName = ord.FileName;
            }
        }

        private async void SelectStoryClick(object sender, RoutedEventArgs e)
        {
            var api = _sharpCloudApiFactory.CreateSharpCloudApi(
                Username.Text,
                Password.Password,
                Url.Text,
                _proxyViewModel.Proxy,
                _proxyViewModel.ProxyAnnonymous,
                _proxyViewModel.ProxyUserName,
                _proxyViewModel.ProxyPassword);

            if (api == null)
            {
                await _logger.Log(InvalidCredentialsException.LoginFailed);
                return;
            }

            var sel = new SelectStory(api, false, Username.Text);

            bool? dialogResult = sel.ShowDialog();
            if (dialogResult == true)
            {
                var story = sel.SelectedStoryLites.First();
                _mainViewModel.SelectedQueryData.StoryId = story.Id;
            }
        }

        private void ViewStoryClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"{Url.Text}/html/#/story/{_mainViewModel.SelectedQueryData.StoryId}");
        }

        private void Database_Engine_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            UrlHelper.GoToAccessDatabaseEngine();
        }

        private void App_Directory_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);
        }

        private void Proxy_OnClick(object sender, RoutedEventArgs e)
        {
            var proxy = new ProxySettings(_proxyViewModel);
            proxy.ShowDialog();
        }

        private void StorySourceSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SourceStorySettings(_mainViewModel.SelectedQueryData)
            {
                Owner = this
            };

            dlg.ShowDialog();
        }

        private void QC_Data_Folder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _localPath);
        }

        private void PublishBatchFolder(
            PasswordSecurity security,
            PublishArchitecture architecture)
        {
            if (!ValidateCredentials())
            {
                return;
            }

            var settings = new PublishSettings
            {
                Data = _mainViewModel.SelectedQueryData,
                Password = Password,
                ProxyViewModel = _proxyViewModel,
                BasePath = _localPath,
                Username = Username.Text,
                SharpCloudUrl = Url.Text,
                PasswordSecurity = security,
                PublishArchitecture = architecture
            };

            _batchPublishHelper.PublishBatchFolder(settings);
        }

        private void PublishBatchFolderClick(object sender, RoutedEventArgs e)
        {
            PublishBatchFolder(
                _mainViewModel.PublishPasswordSecurity,
                _mainViewModel.PublishArchitecture);
        }

        private void QueryItemTreePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
        }

        private void QueryItemTreeMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(this);
                var diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (e.OriginalSource is FrameworkElement element &&
                        element.DataContext is QueryData item)
                    {
                        var dragData = new DataObject(item);
                        DragDrop.DoDragDrop(element, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        private void QueryItemTreeDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(QueryData)) is QueryData source &&
                e.OriginalSource is FrameworkElement fe &&
                fe.DataContext is QueryData dropTarget &&
                source != dropTarget)
            {
                if (dropTarget.IsFolder)
                {
                    dropTarget.DragInto = true;
                }
                else
                {
                    var mousePos = e.GetPosition(this);
                    var diff = _startPoint - mousePos;

                    if (diff.Y > 0)
                    {
                        dropTarget.DragAbove = true;
                    }
                    else if (diff.Y < 0)
                    {
                        dropTarget.DragBelow = true;
                    }
                }
            }

            e.Effects = sender is TreeViewItem
                ? DragDropEffects.Move
                : DragDropEffects.None;
            
            e.Handled = true;
        }

        private void QueryItemTreeDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(QueryData)) is QueryData source &&
                e.OriginalSource is FrameworkElement fe)
            {
                var dropTarget = fe.DataContext as QueryData;

                if (source == dropTarget ||
                    FindParent(dropTarget) == source)
                {
                    ResetDragDropHighlight(dropTarget);
                    return;
                }

                var index = 0;
                var originalSourceParent = FindParent(source);
                originalSourceParent.Connections.Remove(source);
                QueryData updatedSourceParent;

                if (dropTarget != null)
                {
                    ResetDragDropHighlight(dropTarget);

                    if (dropTarget.IsFolder)
                    {
                        updatedSourceParent = dropTarget;
                    }
                    else
                    {
                        var dropParent = FindParent(dropTarget) ?? _queryRootNode;
                        index = dropParent.Connections.IndexOf(dropTarget);
                        updatedSourceParent = dropParent;
                    }
                }
                else
                {
                    index = _queryRootNode.Connections.Count;
                    updatedSourceParent = _queryRootNode;
                }

                var mousePos = e.GetPosition(this);

                if (mousePos.Y > _startPoint.Y)
                {
                    index++;
                }

                if (index > updatedSourceParent.Connections.Count)
                {
                    updatedSourceParent.Connections.Add(source);
                }
                else
                {
                    updatedSourceParent.Connections.Insert(index, source);
                }

                updatedSourceParent.IsExpanded = true;
            }
        }

        private async void RunQueryDataClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData queryData)
            {
                await RunQueryData(queryData);
            }
        }

        private async Task RunAllQueryData(QueryData qd)
        {
            var destination = new QueryDataLoggingDestination(qd);
            await destination.Clear();
            
            _logger.PushLoggingDestination(destination);

            if (qd.IsFolder)
            {
                foreach (var data in qd.Connections)
                {
                    await RunAllQueryData(data);
                }
            }
            else
            {
                var message = $"Running '{qd.Name}'...";
                _mainViewModel.UpdateMessage = message;
                await _logger.Log($"> {message}");
                
                await UpdateSharpCloud(qd);
            }

            _logger.PopLoggingDestination();
        }

        private async Task RunQueryData(QueryData queryData)
        {
            if (!ValidateCredentials() ||
                !ValidateAllStoryIds(queryData))
            {
                return;
            }

            await Task.Delay(20);
            SaveSettings();

            await _logger.Clear();
            await RunAllQueryData(queryData);

            queryData.LastRunDateTime = DateTime.Now;
            SaveSettings();

            _mainViewModel.UpdateMessage = string.Empty;
            await Task.Delay(20);
        }

        private void QueryItemTreeDragLeave(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData queryData)
            {
                queryData.DragAbove = false;
                queryData.DragBelow = false;
                queryData.DragInto = false;
            }
        }

        private static void ResetDragDropHighlight(QueryData queryData)
        {
            queryData.DragAbove = false;
            queryData.DragBelow = false;
            queryData.DragInto = false;
        }
    }
}
