using Microsoft.Win32;
using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Helpers;
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
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly int _maxRowCount;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IItemDataChecker _itemDataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IExcelWriter _excelWriter;
        private readonly MultiDestinationLogger _logger;
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

            _logger = (MultiDestinationLogger) logger;
            _logger.PushLoggingDestination(new TextBoxLoggingDestination(tbResults));
            _logger.PushLoggingDestination(new TextBoxLoggingDestination(tbFolderResults));

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
            
            var queryData = FindQueryData(qd => qd.Id == active)
                            ?? FindQueryData(qd => !qd.IsFolder);

            queryData.IsSelected = true;

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

        private void LoadAllProfiles()
        {
            var file = _localPath + "/connections.json";
            IList<QueryData> connections;

            if (File.Exists(file))
            {
                // load our previous settings
                var encrypted = SaveHelper.DeserializeJSON<IList<QueryData>>(File.ReadAllText(file));
                var filtered = encrypted?.Where(qd => qd != null);
                var decrypted = CreateDecryptedPasswordConnections(filtered);
                connections = new List<QueryData>(decrypted);
            }
            else
            {
                // add some examples

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

            var version = SaveHelper.RegRead(SaveHelper.VersionKey, string.Empty);
            var migrate = false;

            if (!string.IsNullOrWhiteSpace(version))
            {
                var regex = new Regex(@"^.* v([0-9])+\.([0-9])+\.([0-9])+\.([0-9])+$");
                var match = regex.Match(version);
                int.TryParse(match.Groups[1].Value, out var major);
                int.TryParse(match.Groups[2].Value, out var minor);
                int.TryParse(match.Groups[3].Value, out var patch);

                migrate =
                    major > 3 ||
                    minor > 7 ||
                    patch > 1;
            }

            if (migrate)
            {
                var defaultFolder = CreateNewFolder("My Connections");
                defaultFolder.IsExpanded = true;
                defaultFolder.Connections = new ObservableCollection<QueryData>(connections);

                Connections = new ObservableCollection<QueryData>(new[]
                {
                    defaultFolder
                });
            }
            else
            {
                Connections = new ObservableCollection<QueryData>(connections);
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
            
            SaveHelper.RegWrite(SaveHelper.VersionKey, _qcHelper.AppNameOnly);
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
            Process.Start(GetFolder(SelectedQueryData.Name));
        }

        private string GetFolder(string queryName)
        {
            var folder = $"{_localPath}/data";
            Directory.CreateDirectory(folder);
            folder += "/" + queryName;
            Directory.CreateDirectory(folder);
            return folder;
        }

        private string GenerateBatchExe(bool b32Bit, string connectionString, string sequenceName, QueryData queryData)
        {
            var suffix = GetFileSuffix(b32Bit);
            var zipfile = $"SCSQLBatch{suffix}.zip";
            
            var outputFolder = GetFolder(Path.Combine(sequenceName, queryData.Name));

            if (!ValidateCredentials())
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
            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData);
            
            if (index < parent.Connections.Count - 1)
            {
                parent.Connections.Move(index, index + 1);
            }
        }

        private void MoveConnectionUp(object sender, RoutedEventArgs e)
        {
            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData);

            if (index > 0)
            {
                parent.Connections.Move(index, index - 1);
            }
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var queryData = new QueryData(SelectedQueryData);
            AddQueryData(queryData);
        }

        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            var parent = FindParent(SelectedQueryData);
            var index = parent.Connections.IndexOf(SelectedQueryData) - 1;

            var toSelect = index > -1
                ? parent.Connections[index]
                : parent;
            
            parent.Connections.Remove(SelectedQueryData);
            
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

        private void PublishBatchFolderClick32(object sender, RoutedEventArgs e)
        {
            PublishBatchFolder(SelectedQueryData, true);
        }

        private void PublishBatchFolderClick64(object sender, RoutedEventArgs e)
        {
            PublishBatchFolder(SelectedQueryData, false);
        }

        private void PublishBatchFolderClickAuto(object sender, RoutedEventArgs e)
        {
            PublishBatchFolder(SelectedQueryData, DetectIs32Bit);
        }

        private void WriteBatchFile(string path, string filename, string content)
        {
            var sequenceFolder = GetFolder(path);
            var batchPath = Path.Combine(sequenceFolder, $"{filename}.bat");
            File.WriteAllText(batchPath, content);
        }

        private void PublishAllBatchFolders(QueryData queryData, bool is32Bit, string parentPath, StringBuilder parentStringBuilder)
        {
            if (queryData.IsFolder)
            {
                var subPath = Path.Combine(parentPath, queryData.Name);
                var localStringBuilder = new StringBuilder();
                localStringBuilder.AppendLine("@echo off");

                parentStringBuilder.AppendLine($"echo Running: {queryData.Name}");

                foreach (var c in queryData.Connections)
                {
                    PublishAllBatchFolders(c, is32Bit, subPath, localStringBuilder);
                    
                    var batchFolderRoot = GetFolder(subPath);
                    var batchPath = Path.Combine(batchFolderRoot, c.Name, c.Name);
                    parentStringBuilder.AppendLine($"call \"{batchPath}.bat\"");
                }

                WriteBatchFile(subPath, queryData.Name, localStringBuilder.ToString());
            }
            else
            {
                GetFolder(parentPath);

                var fullPath = GenerateBatchExe(
                    is32Bit,
                    queryData.ConnectionsString,
                    parentPath,
                    queryData);

                var suffix = GetFileSuffix(is32Bit);
                var filename = $"SCSQLBatch{suffix}.exe";
                parentStringBuilder.AppendLine($"echo Running: {queryData.Name}");
                parentStringBuilder.AppendLine($"\"{Path.Combine(fullPath, filename)}\"");
            }
        }

        private void PublishBatchFolder(QueryData queryData, bool is32Bit)
        {
            try
            {
                var outputFolder = GetFolder(queryData.Name);

                var notEmpty = Directory.EnumerateFileSystemEntries(outputFolder).Any();
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

                Directory.Delete(outputFolder, true);
                GetFolder(queryData.Name);

                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                PublishAllBatchFolders(queryData, is32Bit, string.Empty, sb);
                WriteBatchFile(queryData.Name, queryData.Name, sb.ToString());
                Process.Start(outputFolder);
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
                await _logger.Log($"--- Running '{qd.Name}'");
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

            UpdatingMessageVisibility = Visibility.Visible;
            await Task.Delay(20);
            SaveSettings();

            await _logger.Clear();
            await RunAllQueryData(queryData);

            queryData.LastRunDateTime = DateTime.Now;
            SaveSettings();

            UpdatingMessageVisibility = Visibility.Collapsed;
            await Task.Delay(20);
        }
    }
}
