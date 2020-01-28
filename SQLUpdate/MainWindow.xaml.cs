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

        private bool _buildRelationships = false;
        private bool _unpublishItems = false;
        private Point _startPoint;
        private Visibility _updatingMessageVisibility = Visibility.Collapsed;
        private Visibility _connectionStringVisibility = Visibility.Visible;
        private Visibility _filenameVisibility = Visibility.Collapsed;
        private Visibility _sharepointVisibility = Visibility.Collapsed;
        private Visibility _sourceStoryIdVisibility = Visibility.Collapsed;
        private Visibility _rewriteDataSourceVisibility = Visibility.Collapsed;

        private Visibility _queryConfigVisibility = Visibility.Collapsed;
        private Visibility _folderConfigVisibility = Visibility.Collapsed;

        private readonly QueryBatch _queryRootNode = new QueryBatch
        {
            Id = QueryBatch.RootId
        };

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

        public bool BuildRelationships
        {
            get => _buildRelationships;
            set
            {
                _buildRelationships = value;
                OnPropertyChanged("BuildRelationships");
            }
        }

        public bool UnpublishItems
        {
            get => _unpublishItems;
            set
            {
                _unpublishItems = value;
                OnPropertyChanged("UnpublishItems");
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

        public Visibility QueryConfigVisibility
        {
            get => _queryConfigVisibility;

            set
            {
                if (_queryConfigVisibility != value)
                {
                    _queryConfigVisibility = value;
                    OnPropertyChanged(nameof(QueryConfigVisibility));
                }
            }
        }

        public Visibility FolderConfigVisibility
        {
            get => _folderConfigVisibility;

            set
            {
                if (_folderConfigVisibility != value)
                {
                    _folderConfigVisibility = value;
                    OnPropertyChanged(nameof(FolderConfigVisibility));
                }
            }
        }

        public ObservableCollection<IQueryItem> Connections
        {
            get => _connections;

            set
            {
                if (_connections != value)
                {
                    _connections = value;
                    OnPropertyChanged(nameof(Connections));

                    _queryRootNode.Connections = _connections;

                    var first = Connections.FirstOrDefault();
                    if (first != null)
                    {
                        SetSelectedQueryItem(first.Id);
                    }
                }
            }
        }

        private static IQueryItem FindSelectedQueryItem(IQueryItem item)
        {
            if (item.IsSelected)
            {
                return item;
            }
            
            if (item is QueryBatch qb)
            {
                return qb.Connections.FirstOrDefault(c => FindSelectedQueryItem(c) != null);
            }

            return null;
        }

        private static void SelectQueryItem(IQueryItem item, string id)
        {
            if (item is QueryData qd)
            {
                qd.IsSelected = qd.Id == id;
            }

            if (item is QueryBatch qb)
            {
                foreach (var c in qb.Connections)
                {
                    SelectQueryItem(c, id);
                }
            }
        }

        private void SetSelectedQueryItem(string id)
        {
            foreach (var c in Connections)
            {
                SelectQueryItem(c, id);
            }
        }

        public IQueryItem SelectedQueryItem => Connections.FirstOrDefault(c => FindSelectedQueryItem(c) != null);

        private QueryData SelectedQueryData => (QueryData) SelectedQueryItem;

        private string _lastUsedSharpCloudConnection;
        private ProxyViewModel _proxyViewModel;
        private ObservableCollection<IQueryItem> _connections;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly ISolutionViewModel _solutionViewModel;
        private readonly IConnectionNameValidator _connectionNameValidator;
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
            ISolutionViewModel solutionViewModel,
            IConnectionNameValidator connectionNameValidator,
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

            _solutionViewModel = solutionViewModel;
            _connectionNameValidator = connectionNameValidator;
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
            ((UILogger) _logger).Output = tbResults;

            _qcHelper = qcHelper;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionString.Text = SaveHelper.RegRead("ConnectionString",
                "Server=.; Integrated Security=true; Database=demo");
            SQLString.Text = SaveHelper.RegRead("SQLString", "SELECT * FROM TABLE");
            Url.Text = SaveHelper.RegRead("URL", "https://my.sharpcloud.com");
            Username.Text = SaveHelper.RegRead("Username", "");
            StoryId.Text = SaveHelper.RegRead("StoryID", "");

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
            _solutionViewModel.Solutions = LoadSolutions();

            // choose our last settings
            var active = SaveHelper.RegRead("ActiveConnection", string.Empty);
            SetSelectedQueryItem(active);
            
            BrowserTabs.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveTab", "0")));

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

        private ObservableCollection<Solution> LoadSolutions()
        {
            var file = _localPath + "/solutions.json";

            if (!File.Exists(file))
            {
                return new ObservableCollection<Solution>();
            }

            var solutions = SaveHelper.DeserializeJSON<IList<Solution>>(File.ReadAllText(file));
            var collection = new ObservableCollection<Solution>(solutions);
            return collection;
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
                Connections = new ObservableCollection<IQueryItem>(decrypted);
            }
            else
            {
                // create some sample settings
                if (SQLString.Text != "SELECT * FROM TABLE" &&
                    ConnectionString.Text != "Server=.; Integrated Security=true; Database=demo")
                {
                    // user probably already has setting we should save from last time
                    // create one based on old settings - which are already set up
                    var pqd = new QueryData((DatabaseType) Int32.Parse(SaveHelper.RegRead("DBType", "0")));
                    pqd.Name = "Previous Connection";
                    pqd.ConnectionsString = SaveHelper.RegRead("ConnectionString",
                        "Server=.; Integrated Security=true; Database=demo");
                    pqd.QueryString = SaveHelper.RegRead("SQLString", "SELECT * FROM TABLE");
                    pqd.StoryId = StoryId.Text;
                    _connections.Add(pqd);
                }

                // add some examples

                Connections = new ObservableCollection<IQueryItem>(new[]
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
                SQLString.Text,
                _itemDataChecker,
                DataGrid,
                d => d.QueryResults);
        }

        private async void RunClickRels(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                SelectedQueryData,
                SQLStringRels.Text,
                _relationshipsChecker,
                DataGridRels,
                d => d.QueryResultsRels);
        }

        private async void PreviewResourceUrlsClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                SelectedQueryData,
                SqlStringResourceUrls.Text,
                _resourceUrlDataChecker,
                DataGridResourceUrls,
                d => d.QueryResultsResourceUrls);
        }

        private async void PreviewPanelsClick(object sender, RoutedEventArgs e)
        {
            await PreviewSql(
                SelectedQueryData,
                SqlStringPanels.Text,
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

            SaveSettings(SelectedQueryData);

            Directory.CreateDirectory(_localPath);
            
            var connections = CreateEncryptedPasswordConnections(_connections);
            var connectionsJson = SaveHelper.SerializeJSON(connections);
            File.WriteAllText(_localPath + "/connections.json", connectionsJson);

            var solutionsJson = SaveHelper.SerializeJSON(_solutionViewModel.Solutions);
            File.WriteAllText(_localPath + "/solutions.json", solutionsJson);

            if (SelectedQueryItem != null)
            {
                SaveHelper.RegWrite("ActiveConnection", SelectedQueryItem.Id);
            }

            SaveHelper.RegWrite("ActiveTab", BrowserTabs.SelectedIndex.ToString());

            SaveHelper.RegWrite("UnpublishItems", UnpublishItems.ToString());

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

        private ObservableCollection<QueryData> CreateEncryptedPasswordConnections(ObservableCollection<IQueryItem> connections)
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

        private void SaveSettings(QueryData qd)
        {
            if (qd != null)
            {
                var nameError = _connectionNameValidator.Validate(ConnectionName.Text);
                var nameIsValid = string.IsNullOrWhiteSpace(nameError);
                
                if (nameIsValid)
                {
                    qd.Name = ConnectionName.Text;
                }
                else
                {
                    ConnectionName.Text = qd.Name;
                    MessageBox.Show(nameError);
                }

                qd.BuildRelationships = BuildRelationships;
                qd.Description = ConnectionDescription.Text;
                qd.ConnectionsString = ConnectionString.Text;
                qd.QueryString = SQLString.Text;
                qd.StoryId = StoryId.Text;
                qd.QueryStringRels = SQLStringRels.Text;
                qd.FileName = FileName.Text;
                qd.SharePointURL = SharePointURL.Text;
                qd.SourceStoryId = SourceStoryId.Text;
                qd.QueryStringPanels = SqlStringPanels.Text;
                qd.QueryStringResourceUrls = SqlStringResourceUrls.Text;
                qd.UnpublishItems = UnpublishItems;
            }
        }

        private bool ValidateCreds()
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
            if (string.IsNullOrEmpty(StoryId.Text))
            {
                MessageBox.Show("Please enter a story ID");
                StoryId.Focus();
                return false;
            }
            Guid test;
            if ( !Guid.TryParse(StoryId.Text, out test))
            {
                MessageBox.Show("Story ID must be a GUID");
                StoryId.Focus();
                return false;
            }
            return true;
        }

        private async void UpdateSharpCloud(object sender, RoutedEventArgs e)
        {
            await UpdateSharpCloud(SelectedQueryData);
        }

        private async Task UpdateSharpCloud(QueryData queryData)
        {
            if (!ValidateCreds())
                return;

            UpdatingMessageVisibility = Visibility.Visible;
            await Task.Delay(20);
            SaveSettings();

            await _logger.Clear();
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
                TargetStoryId = StoryId.Text,
                QueryString = SQLString.Text,
                QueryStringPanels = SqlStringPanels.Text,
                QueryStringRels = SQLStringRels.Text,
                QueryStringResourceUrls = SqlStringResourceUrls.Text,
                ConnectionString = queryData.FormattedConnectionString,
                DBType = queryData.ConnectionType,
                MaxRowCount = maxRowCount,
                UnpublishItems = UnpublishItems,
                BuildRelationships = BuildRelationships
            };

            await _qcHelper.UpdateSharpCloud(config, settings);

            queryData.LogData = tbResults.Text;
            queryData.LastRunDateTime = DateTime.Now;
            tbLastRun.Text = queryData.LastRunDate;
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
            var outputFolder = GenerateBatchFile(b32Bit, ConnectionString.Text, string.Empty, SelectedQueryData);
            Process.Start(outputFolder);
        }

        private string GenerateBatchFile(bool b32Bit, string connectionString, string sequenceName, QueryData queryData)
        {
            var suffix = GetFileSuffix(b32Bit);
            var zipfile = $"SCSQLBatch{suffix}.zip";
            
            var outputFolder = GetFolder(Path.Combine(sequenceName, queryData.Name));

            if (!ValidateCreds())
                return null;

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
                content = ReplaceConfigSetting(content, "00000000-0000-0000-0000-000000000000", StoryId.Text);
                content = ReplaceConfigSetting(content, "SQL", queryData.GetBatchDBType);
                content = ReplaceConfigSetting(content, "CONNECTIONSTRING", formattedConnection.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "QUERYSTRING", queryData.QueryString.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "QUERYRELSSTRING", queryData.QueryStringRels.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "LOGFILE", $"Logfile.txt");
                content = ReplaceConfigSetting(content, "BUILDRELATIONSHIPS", BuildRelationships.ToString());
                content = ReplaceConfigSetting(content, "UNPUBLISHITEMS", UnpublishItems.ToString());
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
                SetSelectedQueryItem(queryData.Id);
                BrowserTabs.SelectedIndex = 0; // go back to the first tab
            }
        }

        private void NewQueryFolderClick(object sender, RoutedEventArgs e)
        {
            var queryBatch = new QueryBatch();
            Connections.Add(queryBatch);
            SetSelectedQueryItem(queryBatch.Id);
            BrowserTabs.SelectedIndex = 0; // go back to the first tab
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var queryData = new QueryData(SelectedQueryData);
            _connections.Add(queryData);
            SetSelectedQueryItem(queryData.Id);
            BrowserTabs.SelectedIndex = 0; // go back to the  first tab
        }

        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            Connections.Remove(SelectedQueryItem);
        }

        private void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is QueryData queryData)
            {
                BuildRelationships = queryData.BuildRelationships;
                ConnectionName.Text = queryData.Name;
                ConnectionDescription.Text = queryData.Description;
                txtDatabaseType.Text = queryData.ConnectionType.ToString();
                ConnectionString.Text = queryData.ConnectionsString;
                SQLString.Text = queryData.QueryString;
                StoryId.Text = queryData.StoryId;
                SQLStringRels.Text = queryData.QueryStringRels;
                FileName.Text = queryData.FileName;
                SharePointURL.Text = queryData.SharePointURL;
                txtExampleRels.Text = "Example: " + queryData.GetExampleRelQuery;
                SourceStoryId.Text = queryData.SourceStoryId;
                SqlStringPanels.Text = queryData.QueryStringPanels;
                SqlStringResourceUrls.Text = queryData.QueryStringResourceUrls;
                UnpublishItems = queryData.UnpublishItems;

                tbLastRun.Text = queryData.LastRunDate;
                tbResults.Text = queryData.LogData;

                DataGrid.ItemsSource = queryData.QueryResults;
                DataGridRels.ItemsSource = queryData.QueryResultsRels;

                SetVisibleObjects(queryData);

                FolderConfigVisibility = Visibility.Collapsed;
                QueryConfigVisibility = Visibility.Visible;
            }
            else if (e.NewValue is QueryBatch)
            {
                FolderConfigVisibility = Visibility.Visible;
                QueryConfigVisibility = Visibility.Collapsed;
            }
        }

        private void SaveSettingsOnLostFocus(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void LostFocusStoryID(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var idString = textBox.Text;
            textBox.Text = _qcHelper.GetStoryUrl(idString);
            SaveSettings();
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

        private void Sharepoint_LostFocus(object sender, RoutedEventArgs e)
        {
            // note - the list ID can be found by reading data from the following url
            // SharePointURL/_api/web/lists
            // TODO give the user a way of seacrching for the list ID..
            // currently this can easily be done from Excel.

            var text = SharePointURL.Text;
            if (text.ToUpper().Contains("LIST="))
            {
                // user has provided a good link with a specified list.
                // make sure the list is a guid 
                text = text.Replace("%7B", "{").Replace("%2D", "-").Replace("%7D", "}");
            }
            SelectedQueryData.SharePointURL = text;
            SharePointURL.Text = text;

            SaveSettings();
        }

        private void ConnectionString_LostFocusName(object sender, RoutedEventArgs e)
        {
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
                FileName.Text = ord.FileName;
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
                StoryId.Text = story.Id;
            }
        }

        private void ViewStoryClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"{Url.Text}/html/#/story/{StoryId.Text}");
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

        private void PublishSolutionClick32(object sender, RoutedEventArgs e)
        {
            PublishSolution(true);
        }

        private void PublishSolutionClick64(object sender, RoutedEventArgs e)
        {
            PublishSolution(false);
        }

        private void PublishSolutionClickAuto(object sender, RoutedEventArgs e)
        {
            PublishSolution(DetectIs32Bit);
        }

        private void PublishSolution(bool is32Bit)
        {
            try
            {
                var nameError = _connectionNameValidator.Validate(_solutionViewModel.SelectedSolution.Name);
                var nameIsValid = string.IsNullOrWhiteSpace(nameError);

                if (!nameIsValid)
                {
                    MessageBox.Show(nameError);
                    return;
                }

                var sb = new StringBuilder();
                var sequenceFolder = GetFolder(_solutionViewModel.SelectedSolution.Name);

                var notEmpty = Directory.EnumerateFileSystemEntries(sequenceFolder).Any();
                if (notEmpty)
                {
                    var result = MessageBox.Show(
                        $"A folder named '{_solutionViewModel.SelectedSolution.Name}' already exist at this location, Do you want to replace?",
                        "WARNING",
                        MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                Directory.Delete(sequenceFolder, true);
                GetFolder(_solutionViewModel.SelectedSolution.Name);

                var connections = _solutionViewModel.IncludedConnections.Cast<QueryData>();
                sb.AppendLine("@echo off");

                foreach (var connection in connections)
                {
                    var path = GenerateBatchFile(
                        is32Bit,
                        connection.ConnectionsString,
                        _solutionViewModel.SelectedSolution.Name,
                        connection);

                    var suffix = GetFileSuffix(is32Bit);
                    var filename = $"SCSQLBatch{suffix}.exe";
                    sb.AppendLine($"echo Running: {connection.Name}");
                    sb.AppendLine($"\"{Path.Combine(path, filename)}\"");
                }

                var batchPath = Path.Combine(sequenceFolder, $"{_solutionViewModel.SelectedSolution.Name}.bat");
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
                        element.DataContext is IQueryItem item)
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
            IQueryItem source = null;

            if (e.Data.GetData(typeof(QueryData)) is QueryData qd)
            {
                source = qd;
            }
            
            if (e.Data.GetData(typeof(QueryBatch)) is QueryBatch qb)
            {
                source = qb;
            }

            if (source != null &&
                e.OriginalSource is FrameworkElement fe)
            {
                var index = 0;
                var dropTarget = fe.DataContext as IQueryItem;
                var sourceParent = source.ParentFolder ?? _queryRootNode;
                sourceParent.Connections.Remove(source);

                if (dropTarget is QueryData)
                {
                    var dropParent = dropTarget.ParentFolder ?? _queryRootNode;
                    index = dropParent.Connections.IndexOf(dropTarget);
                    source.ParentFolder = dropParent;
                }
                else if (dropTarget is QueryBatch qbTarget)
                {
                    source.ParentFolder = qbTarget;
                }
                else
                {
                    index = _queryRootNode.Connections.Count;
                    source.ParentFolder = _queryRootNode;
                }

                var mousePos = e.GetPosition(this);

                if (mousePos.Y > _startPoint.Y)
                {
                    index++;
                }

                if (index > source.ParentFolder.Connections.Count)
                {
                    source.ParentFolder.Connections.Add(source);
                }
                else
                {
                    source.ParentFolder.Connections.Insert(index, source);
                }
            }
        }
    }
}
