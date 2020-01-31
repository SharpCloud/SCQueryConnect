using Microsoft.Win32;
using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
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
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Directory = System.IO.Directory;
using MessageBox = System.Windows.MessageBox;

namespace SCQueryConnect
{
    // some useful links... https://www.microsoft.com/en-us/download/details.aspx?id=13255 for accessing the access db

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static bool DetectIs32Bit => IntPtr.Size == 4;
        private static string GetFileSuffix(bool b32bit) => b32bit ? "x86" : string.Empty;

        private Point _startPoint;
        private Visibility _updatingMessageVisibility = Visibility.Collapsed;
        private Visibility _connectionStringVisibility = Visibility.Visible;
        private Visibility _filenameVisibility = Visibility.Collapsed;
        private Visibility _sharepointVisibility = Visibility.Collapsed;
        private Visibility _sourceStoryIdVisibility = Visibility.Collapsed;
        private Visibility _rewriteDataSourceVisibility = Visibility.Collapsed;

        private readonly QueryData _queryRootNode;

        public string AppName
        {
            get
            {
                return _qcHelper.AppName;
            }
        }

        public Visibility UpdatingMessageVisibility
        {
            get => _updatingMessageVisibility;
            set
            {
                _updatingMessageVisibility = value;
                OnPropertyChanged("UpdatingMessageVisibility");
            }
        }

        public Visibility ConnectionStringVisibility
        {
            get => _connectionStringVisibility;

            set
            {
                if (_connectionStringVisibility != value)
                {
                    _connectionStringVisibility = value;
                    OnPropertyChanged(nameof(ConnectionStringVisibility));
                }
            }
        }

        public Visibility FilenameVisibility
        {
            get => _filenameVisibility;

            set
            {
                if (_filenameVisibility != value)
                {
                    _filenameVisibility = value;
                    OnPropertyChanged(nameof(FilenameVisibility));
                }
            }
        }

        public Visibility SharepointVisibility
        {
            get => _sharepointVisibility;

            set
            {
                if (_sharepointVisibility != value)
                {
                    _sharepointVisibility = value;
                    OnPropertyChanged(nameof(SharepointVisibility));
                }
            }
        }

        public Visibility SourceStoryIdVisibility
        {
            get => _sourceStoryIdVisibility;

            set
            {
                if (_sourceStoryIdVisibility != value)
                {
                    _sourceStoryIdVisibility = value;
                    OnPropertyChanged(nameof(SourceStoryIdVisibility));
                }
            }
        }

        public Visibility RewriteDataSourceVisibility
        {
            get => _rewriteDataSourceVisibility;

            set
            {
                if (_rewriteDataSourceVisibility != value)
                {
                    _rewriteDataSourceVisibility = value;
                    OnPropertyChanged(nameof(RewriteDataSourceVisibility));
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

        private void SelectQueryData(QueryData queryData)
        {
            if (queryData == null)
            {
                return;
            }

            queryData.IsSelected = true;
            SelectedQueryData = queryData;

            var hierarchy = new List<QueryData>();

            do
            {
                hierarchy.Add(queryData);
                queryData = FindParent(queryData);
            }
            while (queryData != null);

            var startIndex = hierarchy.Count - 2;

            var treeViewItem = QueryItemTree.ItemContainerGenerator
                .ContainerFromItem(hierarchy[startIndex]) as TreeViewItem;

            for (var i = startIndex - 1; treeViewItem != null && i >= 0; i--)
            {
                treeViewItem = treeViewItem.ItemContainerGenerator
                    .ContainerFromItem(hierarchy[i]) as TreeViewItem;
            }

            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
            }
        }

        private QueryData _selectedQueryData;
        
        public QueryData SelectedQueryData
        {
            get => _selectedQueryData;

            set
            {
                if (_selectedQueryData != value)
                {
                    _selectedQueryData = value;
                    OnPropertyChanged(nameof(SelectedQueryData));
                }
            }
        }

        private string _lastUsedSharpCloudConnection;
        private ProxyViewModel _proxyViewModel;
        private ObservableCollection<QueryData> _connections;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IItemDataChecker _itemDataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IExcelWriter _excelWriter;
        private readonly ILog _logger;
        private readonly IRelationshipsDataChecker _relationshipsChecker;
        private readonly ISharpCloudApiFactory _sharpCloudApiFactory;
        private readonly IPanelsDataChecker _panelsDataChecker;
        private readonly IResourceUrlDataChecker _resourceUrlDataChecker;
        private readonly string _localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");

        public MainWindow(
            IConnectionStringHelper connectionStringHelper,
            IItemDataChecker itemDataChecker,
            IDbConnectionFactory dbConnectionFactory,
            IEncryptionHelper encryptionHelper,
            IExcelWriter excelWriter,
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

            _queryRootNode = CreateNewFolder(QueryData.RootId);
            _queryRootNode.Id = QueryData.RootId;

            _connectionStringHelper = connectionStringHelper;

            _itemDataChecker = itemDataChecker;
            _itemDataChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterr);

            _dbConnectionFactory = dbConnectionFactory;
            _encryptionHelper = encryptionHelper;
            _excelWriter = excelWriter;

            _relationshipsChecker = relationshipsDataChecker;
            _relationshipsChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterrRels);

            _sharpCloudApiFactory = sharpCloudApiFactory;

            _panelsDataChecker = panelsDataChecker;
            _panelsDataChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterrPanels);

            _resourceUrlDataChecker = resourceUrlDataChecker;
            _resourceUrlDataChecker.ValidityProcessor = new UIDataCheckerValidityProcessor(txterrResourceUrls);

            _logger = logger;
            ((UILogger) _logger).Initialise(tbResults, tbFolderResults);

            _qcHelper = qcHelper;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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

            LoadAllProfiles();

            // choose our last settings
            var active = SaveHelper.RegRead("ActiveConnection", string.Empty);
            var queryData = FindQueryData(qd => qd.Id == active);
            SelectQueryData(queryData);

            BrowserTabs.SelectedIndex = (int.Parse(SaveHelper.RegRead("ActiveTab", "0")));

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

        private static async Task RecursivelyApply(
            QueryData queryData,
            bool excludeFolders,
            Action<QueryData> action)
        {
            if (!excludeFolders || !queryData.IsFolder)
            {
                action(queryData);
            }

            if (queryData.IsFolder)
            {
                foreach (var c in queryData.Connections)
                {
                    await RecursivelyApply(c, excludeFolders, action);
                }
            }
        }

        private void LoadAllProfiles()
        {
            var file = _localPath + "/connections.json";

            if (File.Exists(file))
            {
                // load our previous settings
                var encrypted = SaveHelper.DeserializeJSON<IList<QueryData>>(File.ReadAllText(file));
                var filtered = encrypted?.Where(qd => qd != null);
                var decrypted = CreateDecryptedPasswordConnections(filtered);
                Connections = new ObservableCollection<QueryData>(decrypted);
            }
            else
            {
                // add some examples

                Connections = new ObservableCollection<QueryData>(new[]
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
        }

        private void RewriteDataSourceClick(object sender, RoutedEventArgs e)
        {
            RewriteDataSource(SelectedQueryData);
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
            await TestConnection(SelectedQueryData);
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
            ReviewConnection(SelectedQueryData);
        }

        private void ReviewConnection(QueryData queryData)
        {
            var info = string.Format("Internal Connection Type:\n{0}\n\nConnection String:\n{1}",
                queryData.GetBatchDBType,
                queryData.FormattedConnectionString);

            var dlg = new ConnectionInfo(info);
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
                SelectedQueryData,
                SelectedQueryData.QueryString,
                _itemDataChecker,
                DataGrid,
                d => d.QueryResults);
        }

        private async void RunClickRels(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                SelectedQueryData,
                SelectedQueryData.QueryStringRels,
                _relationshipsChecker,
                DataGridRels,
                d => d.QueryResultsRels);
        }

        private async void PreviewResourceUrlsClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                SelectedQueryData,
                SelectedQueryData.QueryStringResourceUrls,
                _resourceUrlDataChecker,
                DataGridResourceUrls,
                d => d.QueryResultsResourceUrls);
        }

        private async void PreviewPanelsClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                SelectedQueryData,
                SelectedQueryData.QueryStringPanels,
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
                            prop.SetValue(SelectedQueryData, dt.DefaultView, null);

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

            Directory.CreateDirectory(_localPath);
            var connections = CreateEncryptedPasswordConnections(_connections);
            var connectionsJson = SaveHelper.SerializeJSON(connections);
            File.WriteAllText(_localPath + "/connections.json", connectionsJson);

            if (SelectedQueryData != null)
            {
                SaveHelper.RegWrite("ActiveConnection", SelectedQueryData.Id);
            }

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
        }

        private ObservableCollection<QueryData> CreateEncryptedPasswordConnections(ObservableCollection<QueryData> connections)
        {
            var newConnections = new ObservableCollection<QueryData>();

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

        private bool ValidateCreds(QueryData queryData)
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
            if (string.IsNullOrEmpty(queryData.StoryId))
            {
                MessageBox.Show("Please enter a story ID");
                StoryId.Focus();
                return false;
            }
            if ( !Guid.TryParse(queryData.StoryId, out _))
            {
                MessageBox.Show("Story ID must be a GUID");
                StoryId.Focus();
                return false;
            }
            return true;
        }

        private async void UpdateSharpCloud(object sender, RoutedEventArgs e)
        {
            await UpdateSharpCloud(SelectedQueryData, true);
        }

        private async Task UpdateSharpCloud(QueryData queryData, bool clearLog)
        {
            if (!ValidateCreds(queryData))
            {
                return;
            }

            UpdatingMessageVisibility = Visibility.Visible;
            await Task.Delay(20);
            SaveSettings();

            if (clearLog)
            {
                await _logger.Clear();
            }

            int maxRowCount;

            try
            {
                maxRowCount = int.Parse(ConfigurationManager.AppSettings["MaxRowCount"]);
            }
            catch (Exception)
            {
                maxRowCount = 1000;
            }

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
                MaxRowCount = maxRowCount,
                UnpublishItems = queryData.UnpublishItems,
                BuildRelationships = queryData.BuildRelationships
            };

            await _qcHelper.UpdateSharpCloud(config, settings);

            queryData.LogData = ((UILogger) _logger).GetLogText();
            queryData.LastRunDateTime = DateTime.Now;
            SaveSettings();

            UpdatingMessageVisibility = Visibility.Collapsed;
            await Task.Delay(20);
        }

        private void ViewExisting(object sender, RoutedEventArgs e)
        {
            Process.Start(GetFolder(SelectedQueryData.Name));
        }

        private void GenerateBatchFile32(object sender, RoutedEventArgs e)
        {
            GenerateBatchFile(true);
        }

        private void GenerateBatchFile64(object sender, RoutedEventArgs e)
        {
            GenerateBatchFile(false);
        }

        private void GenerateBatchFileThis(object sender, RoutedEventArgs e)
        {
            GenerateBatchFile(DetectIs32Bit);
        }

        private string GetFolder(string queryName)
        {
            var folder = $"{_localPath}/data";
            Directory.CreateDirectory(folder);
            folder += "/" + queryName;
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void GenerateBatchFile(bool b32Bit)
        {
            var outputFolder = GenerateBatchFile(b32Bit, SelectedQueryData.ConnectionsString, string.Empty, SelectedQueryData);
            Process.Start(outputFolder);
        }

        private string GenerateBatchFile(bool b32Bit, string connectionString, string sequenceName, QueryData queryData)
        {
            var suffix = GetFileSuffix(b32Bit);
            var zipfile = $"SCSQLBatch{suffix}.zip";
            
            var outputFolder = GetFolder(Path.Combine(sequenceName, queryData.Name));

            if (!ValidateCreds(queryData))
            {
                return null;
            }

            try
            {
                var configFilename = outputFolder + $"/SCSQLBatch{suffix}.exe.config";

                if (File.Exists(configFilename))
                {
                    if (MessageBox.Show("Config files already exist in this location, Do you want to replace?", "WARNING", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        return null;
                }

                if (connectionString.Contains("\""))
                    MessageBox.Show(
                        "Your connection string and/or query string contains '\"', which will automatically be replaced with '");
                try
                {
                    File.Delete($"{outputFolder}/Autofac.dll");
                    File.Delete($"{outputFolder}/Newtonsoft.Json.dll");
                    File.Delete($"{outputFolder}/SC.Framework.dll");
                    File.Delete($"{outputFolder}/SC.API.ComInterop.dll");
                    File.Delete($"{outputFolder}/SC.Api.dll");
                    File.Delete($"{outputFolder}/SC.SharedModels.dll");
                    File.Delete($"{outputFolder}/SCSQLBatch{suffix}.exe");
                    File.Delete($"{outputFolder}/SCSQLBatch{suffix}.exe.config");
                    File.Delete($"{outputFolder}/SCSQLBatch.zip");
                    File.Delete($"{outputFolder}/SCQueryConnect.Common.dll");

                    ZipFile.ExtractToDirectory(zipfile, outputFolder);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Sorry, we were unable to complete the process\r\rError: {e.Message}");
                    return null;
                }

                // set up the config

                // Remove data source if type is SharpCloud; a temp file will
                // be used, so an overwrite prompt will not appear

                var formattedConnection = queryData.GetBatchDBType == DatabaseStrings.SharpCloudExcel
                    ? _connectionStringHelper.SetDataSource(
                        queryData.FormattedConnectionString,
                        string.Empty)
                    : queryData.FormattedConnectionString;

                var passwordBytes = _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(Password.Password),
                    out var entropy,
                    DataProtectionScope.LocalMachine);

                var proxyPasswordBytes = _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(_proxyViewModel.ProxyPassword),
                    out var proxyEntropy,
                    DataProtectionScope.LocalMachine);

                var content = File.ReadAllText(configFilename);

                content = ReplaceConfigSetting(content, "USERID", Username.Text);
                content = ReplaceConfigSetting(content, "PASSWORD_DPAPI", Convert.ToBase64String(passwordBytes));
                content = ReplaceConfigSetting(content, "PASSWORD_DPAPI_ENTROPY", Convert.ToBase64String(entropy));
                content = ReplaceConfigSetting(content, "https://my.sharpcloud.com", Url.Text);
                content = ReplaceConfigSetting(content, "00000000-0000-0000-0000-000000000000", queryData.StoryId);
                content = ReplaceConfigSetting(content, "SQL", queryData.GetBatchDBType);
                content = ReplaceConfigSetting(content, "CONNECTIONSTRING", formattedConnection.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "QUERYSTRING", queryData.QueryString.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "QUERYRELSSTRING", queryData.QueryStringRels.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "LOGFILE", $"Logfile.txt");
                content = ReplaceConfigSetting(content, "BUILDRELATIONSHIPS", queryData.BuildRelationships.ToString());
                content = ReplaceConfigSetting(content, "UNPUBLISHITEMS", queryData.UnpublishItems.ToString());
                content = ReplaceConfigSetting(content, "PROXYADDRESS", _proxyViewModel.Proxy);
                content = ReplaceConfigSetting(content, "PROXYANONYMOUS", _proxyViewModel.ProxyAnnonymous.ToString());
                content = ReplaceConfigSetting(content, "PROXYUSERNAME", _proxyViewModel.ProxyUserName);
                content = ReplaceConfigSetting(content, "PROXYPWORD_DPAPI", Convert.ToBase64String(proxyPasswordBytes));
                content = ReplaceConfigSetting(content, "PROXYPWORD_DPAPI_ENTROPY", Convert.ToBase64String(proxyEntropy));

                File.WriteAllText(configFilename, content);

                // update the Logfile
                var logfile = $"{outputFolder}Logfile.txt";
                var contentNotes = new List<string>();
                contentNotes.Add($"----------------------------------------------------------------------");
                contentNotes.Add(b32Bit
                    ? $"32 bit (x86) Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}"
                    : $"64 bit Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}");
                contentNotes.Add($"----------------------------------------------------------------------");

                File.AppendAllLines(logfile, contentNotes);
                return outputFolder;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            return null;
        }

        private static string ReplaceConfigSetting(
            string configText,
            string oldValue,
            string newValue)
        {
            var updated = configText.Replace($"\"{oldValue}\"", $"\"{newValue}\"");
            return updated;
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
                _connections.Add(queryData);
                SelectQueryData(queryData);
                BrowserTabs.SelectedIndex = 0; // go back to the first tab
            }
        }

        private void NewQueryFolderClick(object sender, RoutedEventArgs e)
        {
            var queryData = CreateNewFolder("New Folder");
            Connections.Add(queryData);
            SelectQueryData(queryData);
            BrowserTabs.SelectedIndex = 0; // go back to the first tab
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var queryData = new QueryData(SelectedQueryData);
            _connections.Add(queryData);
            SelectQueryData(queryData);
            BrowserTabs.SelectedIndex = 0; // go back to the  first tab
        }

        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData) - 1;

            var toSelect = index > -1
                ? parent.Connections[index]
                : parent;
            
            parent.Connections.Remove(SelectedQueryData);
            SelectQueryData(toSelect);
        }

        private void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedQueryData = e.NewValue as QueryData;

            if (SelectedQueryData == null)
            {
                return;
            }

            if (SelectedQueryData.IsFolder)
            {
                tbFolderResults.Text = SelectedQueryData.LogData;
            }
            else
            {
                tbResults.Text = SelectedQueryData.LogData;
                DataGrid.ItemsSource = SelectedQueryData.QueryResults;
                DataGridRels.ItemsSource = SelectedQueryData.QueryResultsRels;
                DataGridResourceUrls.ItemsSource = SelectedQueryData.QueryResultsResourceUrls;
                DataGridPanels.ItemsSource = SelectedQueryData.QueryResultsPanels;

                SetVisibleObjects(SelectedQueryData);
            }
        }

        private void FileName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedQueryData.ConnectionType == DatabaseType.Excel ||
                SelectedQueryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                var filenameBox = sender as TextBox;
                var validated = _excelWriter.GetValidFilename(filenameBox.Text);
                filenameBox.Text = validated;
            }
            SaveSettings();
        }

        private void SetVisibleObjects(QueryData qd)
        {
            if (qd != null)
            {
                switch (qd.ConnectionType)
                {
                    case DatabaseType.Access:
                        ConnectionStringVisibility = Visibility.Collapsed;
                        FilenameVisibility = Visibility.Visible;
                        SharepointVisibility = Visibility.Collapsed;
                        SourceStoryIdVisibility = Visibility.Collapsed;
                        RewriteDataSourceVisibility = Visibility.Collapsed;
                        break;

                    case DatabaseType.Excel: 
                        ConnectionStringVisibility = Visibility.Collapsed;
                        FilenameVisibility = Visibility.Visible;
                        SharepointVisibility = Visibility.Collapsed;
                        SourceStoryIdVisibility = Visibility.Collapsed;
                        RewriteDataSourceVisibility = Visibility.Visible;
                        break;

                    case DatabaseType.SharepointList:
                        ConnectionStringVisibility = Visibility.Collapsed;
                        FilenameVisibility = Visibility.Collapsed;
                        SharepointVisibility = Visibility.Visible;
                        SourceStoryIdVisibility = Visibility.Collapsed;
                        RewriteDataSourceVisibility = Visibility.Collapsed;
                        break;

                    case DatabaseType.SharpCloudExcel:
                        ConnectionStringVisibility = Visibility.Collapsed;
                        FilenameVisibility = Visibility.Visible;
                        SharepointVisibility = Visibility.Collapsed;
                        SourceStoryIdVisibility = Visibility.Visible;
                        RewriteDataSourceVisibility = Visibility.Collapsed;
                        break;

                    default:
                        ConnectionStringVisibility = Visibility.Visible;
                        FilenameVisibility = Visibility.Collapsed;
                        SharepointVisibility = Visibility.Collapsed;
                        SourceStoryIdVisibility = Visibility.Collapsed;
                        RewriteDataSourceVisibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        private void BrowseForDataSourceClick(object sender, RoutedEventArgs e)
        {
            var ord = new OpenFileDialog();

            switch (SelectedQueryData.ConnectionType)
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
                SelectedQueryData.FileName = ord.FileName;
            }
        }

        private void SelectStoryClick(object sender, RoutedEventArgs e)
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
                _logger.Log(InvalidCredentialsException.LoginFailed);
                return;
            }

            var sel = new SelectStory(api, false, Username.Text);

            bool? dialogResult = sel.ShowDialog();
            if (dialogResult == true)
            {
                var story = sel.SelectedStoryLites.First();
                SelectedQueryData.StoryId = story.Id;
            }
        }

        private void ViewStoryClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"{Url.Text}/html/#/story/{SelectedQueryData.StoryId}");
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
            var dlg = new SourceStorySettings(SelectedQueryData)
            {
                Owner = this
            };

            dlg.ShowDialog();
        }

        private void QC_Data_Folder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _localPath);
        }

        private void TbResultsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text.Contains("ERROR"))
                {
                    textBox.Background = Brushes.DarkRed;
                    textBox.Foreground = Brushes.Pink;
                }
                else if (textBox.Text.Contains("WARNING"))
                {
                    textBox.Background = Brushes.DarkGoldenrod;
                    textBox.Foreground = Brushes.LightGoldenrodYellow;
                }
                else
                {
                    textBox.Background = (SolidColorBrush)Application.Current.Resources["QCBackground"];
                    textBox.Foreground = (SolidColorBrush) Application.Current.Resources["QCBlue"];
                }
            }
        }

        private async void PublishBatchFolderClick32(object sender, RoutedEventArgs e)
        {
            await PublishBatchFolder(SelectedQueryData, true);
        }

        private async void PublishBatchFolderClick64(object sender, RoutedEventArgs e)
        {
            await PublishBatchFolder(SelectedQueryData, false);
        }

        private async void PublishBatchFolderClickAuto(object sender, RoutedEventArgs e)
        {
            await PublishBatchFolder(SelectedQueryData, DetectIs32Bit);
        }

        private async Task PublishBatchFolder(QueryData queryData, bool is32Bit)
        {
            try
            {
                var sb = new StringBuilder();
                var sequenceFolder = GetFolder(queryData.Name);

                var notEmpty = Directory.EnumerateFileSystemEntries(sequenceFolder).Any();
                if (notEmpty)
                {
                    var result = MessageBox.Show(
                        $"A folder named '{queryData.Name}' already exist at this location, Do you want to replace?",
                        "WARNING",
                        MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                Directory.Delete(sequenceFolder, true);
                GetFolder(queryData.Name);

                var connections = new List<QueryData>();
                await RecursivelyApply(queryData, true, connections.Add);
                
                sb.AppendLine("@echo off");
                foreach (var connection in connections)
                {
                    var path = GenerateBatchFile(
                        is32Bit,
                        connection.ConnectionsString,
                        queryData.Name,
                        connection);

                    var suffix = GetFileSuffix(is32Bit);
                    var filename = $"SCSQLBatch{suffix}.exe";
                    sb.AppendLine($"echo Running: {connection.Name}");
                    sb.AppendLine($"\"{Path.Combine(path, filename)}\"");
                }

                var batchPath = Path.Combine(sequenceFolder, $"{queryData.Name}.bat");
                var content = sb.ToString();
                File.WriteAllText(batchPath, content);
                Process.Start(sequenceFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorry, we were unable to complete the process\r\rError: {ex.Message}");
            }
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
                if (source == dropTarget)
                {
                    return;
                }

                var index = 0;
                var originalSourceParent = FindParent(source);
                originalSourceParent.Connections.Remove(source);
                QueryData updatedSourceParent;

                if (dropTarget != null)
                {
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

        private async void RunFolderClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData connectionFolder)
            {
                async Task RunUpdates(QueryData queryData)
                {
                    if (queryData.IsFolder)
                    {
                        foreach (var data in queryData.Connections)
                        {
                            await RunUpdates(data);
                        }
                    }
                    else
                    {
                        await _logger.Log($"--- Running '{queryData.Name}'");
                        await UpdateSharpCloud(queryData, false);
                    }
                }

                await RecursivelyApply(connectionFolder, true, async qd => await RunUpdates(qd));

                if (connectionFolder.IsFolder)
                {
                    await RunUpdates(connectionFolder);
                }
                else
                {
                    await _logger.Log("Nothing to do: no connections to run");
                }
            }
        }
    }
}
