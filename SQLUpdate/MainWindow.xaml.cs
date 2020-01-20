using Microsoft.Win32;
using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.ViewModels;
using SCQueryConnect.Views;
using SQLUpdate.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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

        private bool _unpublishItems = false;
        private Visibility _updatingMessageVisibility = Visibility.Collapsed;
        private Visibility _connectionStringVisibility = Visibility.Visible;
        private Visibility _filenameVisibility = Visibility.Collapsed;
        private Visibility _sharepointVisibility = Visibility.Collapsed;
        private Visibility _sourceStoryIdVisibility = Visibility.Collapsed;
        private Visibility _rewriteDataSourceVisibility = Visibility.Collapsed;

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

        public ObservableCollection<QueryData> Connections
        {
            get => _connections;

            set
            {
                if (_connections != value)
                {
                    _connections = value;
                    OnPropertyChanged(nameof(Connections));

                    _solutionViewModel.SetConnections(_connections);
                }
            }
        }

        private QueryData SelectedQueryData => connectionList.SelectedItem as QueryData;

        private string _lastUsedSharpCloudConnection;
        private ProxyViewModel _proxyViewModel;
        private ObservableCollection<QueryData> _connections;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly ISolutionViewModel _solutionViewModel;
        private readonly IConnectionNameValidator _connectionNameValidator;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IDataChecker _dataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IExcelWriter _excelWriter;
        private readonly ILog _logger;
        private readonly IRelationshipsDataChecker _relationshipsChecker;
        private readonly ISharpCloudApiFactory _sharpCloudApiFactory;
        private readonly string _localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");

        public MainWindow(
            ISolutionViewModel solutionViewModel,
            IConnectionNameValidator connectionNameValidator,
            IConnectionStringHelper connectionStringHelper,
            IDataChecker dataChecker,
            IDbConnectionFactory dbConnectionFactory,
            IEncryptionHelper encryptionHelper,
            IExcelWriter excelWriter,
            IRelationshipsDataChecker relationshipsDataChecker,
            ISharpCloudApiFactory sharpCloudApiFactory,
            ILog logger,
            IQueryConnectHelper qcHelper)
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContext = this;

            _solutionViewModel = solutionViewModel;
            _connectionNameValidator = connectionNameValidator;
            _connectionStringHelper = connectionStringHelper;

            _dataChecker = dataChecker;
            ((UIDataChecker) _dataChecker).ErrorText = txterr;

            _dbConnectionFactory = dbConnectionFactory;
            _encryptionHelper = encryptionHelper;
            _excelWriter = excelWriter;

            _relationshipsChecker = relationshipsDataChecker;
            ((UIRelationshipsDataChecker) _relationshipsChecker).RelationshipErrorText = txterrRels;

            _sharpCloudApiFactory = sharpCloudApiFactory;

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

            //cbDatabase.SelectedIndex = Int32.Parse(SaveHelper.RegRead("DBType", "0"));

            LoadAllProfiles();
            connectionList.ItemsSource = _connections;

            // choose our last settings
            connectionList.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveConnection", "0")));
            BrowserTabs.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveTab", "0")));

            bool unpub;
            if (bool.TryParse(SaveHelper.RegRead("UnpublishItems", "false"), out unpub))
                UnpublishItems = unpub;
            else
                UnpublishItems = false;

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

            if (File.Exists(file))
            {
                // load our previous settings
                var loaded = SaveHelper.DeserializeJSON<QueryData[]>(File.ReadAllText(file));
                
                var collection = new SmartObservableCollection<QueryData>();
                collection.AddRange(loaded.Where(qd => qd != null));
                
                var decrypted = CreateDecryptedPasswordConnections(collection);
                Connections = new ObservableCollection<QueryData>(decrypted);
            }
            else
            {   // create some sample settings
                if (SQLString.Text != "SELECT * FROM TABLE" && ConnectionString.Text != "Server=.; Integrated Security=true; Database=demo")
                {
                    // user papbably already has setting we should save from last time
                    // create one based on old settings - which are already set up
                    var pqd = new QueryData((DatabaseType)Int32.Parse(SaveHelper.RegRead("DBType", "0")));
                    pqd.Name = "Previous Connection";
                    pqd.ConnectionsString = SaveHelper.RegRead("ConnectionString", "Server=.; Integrated Security=true; Database=demo");
                    pqd.QueryString = SaveHelper.RegRead("SQLString", "SELECT * FROM TABLE");
                    pqd.StoryId = StoryId.Text;
                    _connections.Add(pqd);
                }

                // add some examples
                _connections.Add(new QueryData(DatabaseType.Excel)); 
                _connections.Add(new QueryData(DatabaseType.Access));
                _connections.Add(new QueryData(DatabaseType.SharepointList));
                _connections.Add(new QueryData(DatabaseType.SQL));
                _connections.Add(new QueryData(DatabaseType.ODBC));
                _connections.Add(new QueryData(DatabaseType.ADO));
                _connections.Add(new QueryData(DatabaseType.SharpCloudExcel));
            }
        }

        private void RewriteDataSourceClick(object sender, RoutedEventArgs e)
        {
            var filepath = _connectionStringHelper.GetVariable(
                SelectedQueryData.FormattedConnectionString,
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

        private IDbConnection GetDb()
        {
            return _dbConnectionFactory.GetDb(
                SelectedQueryData.FormattedConnectionString,
                SelectedQueryData.ConnectionType);
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
            if (SelectedQueryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                try
                {
                    await _qcHelper.InitialiseDatabase(
                        GetApiConfiguration(),
                        SelectedQueryData.FormattedConnectionString,
                        SelectedQueryData.ConnectionType);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not connect to SharpCloud! {ex.Message}");
                    return;
                }
            }

            try
            {
                using (IDbConnection connection = GetDb())
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
            string info = string.Format("Internal Connection Type:\n{0}\n\nConnection String:\n{1}",
                SelectedQueryData.GetBatchDBType,
                SelectedQueryData.FormattedConnectionString);

            var dlg = new ConnectionInfo(info);
            dlg.ShowDialog();
            //MessageBox.Show(info, "Internal Connection Info");
        }

        private void SetLastUsedSharpCloudConnection()
        {
            if (SelectedQueryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                _lastUsedSharpCloudConnection = SelectedQueryData.FormattedConnectionString;
            }
        }

        /// <summary>
        /// Check if updating the database is necessary, e.g. an update is only necessary
        /// when running queries against data when the query is run for the first time;
        /// subsequent runs should be able to use local data to speed up the process.
        /// </summary>
        private async Task InitialiseSharpCloudDataIfNeeded()
        {
            if (SelectedQueryData.ConnectionType == DatabaseType.SharpCloudExcel &&
                SelectedQueryData.FormattedConnectionString != _lastUsedSharpCloudConnection)
            {
                await _qcHelper.InitialiseDatabase(
                    GetApiConfiguration(),
                    SelectedQueryData.FormattedConnectionString,
                    SelectedQueryData.ConnectionType);
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
            try
            {
                await InitialiseSharpCloudDataIfNeeded();

                using (IDbConnection connection = GetDb())
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {

                        if (connection is OleDbConnection) { 
                            DataTable schemaTable = ((OleDbConnection)connection).GetOleDbSchemaTable(
                                OleDbSchemaGuid.Tables,
                                new object[] {null, null, null, "TABLE"});

                            Debug.Write( schemaTable.ToString());
                        }



                        command.CommandText = SQLString.Text;
                        command.CommandType = CommandType.Text;

                        using (IDataReader reader = command.ExecuteReader())
                        {
                            _dataChecker.CheckDataIsOK(reader);

                            DataTable dt = new DataTable();
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

                            DataGrid.ItemsSource = dt.DefaultView;
                            SelectedQueryData.QueryResults = dt.DefaultView;

                            for (var col = 0; col < DataGrid.Columns.Count; col++)
                            {
                                DataGrid.Columns[col].Header = dt.Columns[col].Caption;
                            }
                        }
                    }
                }

                SetLastUsedSharpCloudConnection();
            }
            catch (Exception ex)
            {
                ProcessRunException(ex);
            }
        }

        private async void RunClickRels(object sender, RoutedEventArgs e)
        {
            await InitialiseSharpCloudDataIfNeeded();

            try
            {
                using (IDbConnection connection = GetDb())
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = SQLStringRels.Text;
                        command.CommandType = CommandType.Text;

                        using (IDataReader reader = command.ExecuteReader())
                        {
                            _relationshipsChecker.CheckDataIsOKRels(reader);

                            DataTable dt = new DataTable();
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

                            DataGridRels.ItemsSource = dt.DefaultView;
                            SelectedQueryData.QueryResultsRels = dt.DefaultView;

                            for (var col = 0; col < DataGridRels.Columns.Count; col++)
                            {
                                DataGridRels.Columns[col].Header = dt.Columns[col].Caption;
                            }
                        }
                    }
                }

                SetLastUsedSharpCloudConnection();
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
            var toSave = CreateEncryptedPasswordConnections(_connections);
            var s = SaveHelper.SerializeJSON(toSave);
            File.WriteAllText(_localPath + "/connections.json", s);

            SaveHelper.RegWrite("ActiveConnection", connectionList.SelectedIndex.ToString());
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
                
                qd.Description = ConnectionDescription.Text;
                qd.ConnectionsString = ConnectionString.Text;
                qd.QueryString = SQLString.Text;
                qd.StoryId = StoryId.Text;
                qd.QueryStringRels = SQLStringRels.Text;
                qd.FileName = FileName.Text;
                qd.SharePointURL = SharePointURL.Text;
                qd.SourceStoryId = SourceStoryId.Text;
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
            if (!ValidateCreds())
                return;

            UpdatingMessageVisibility = Visibility.Visible;
            await Task.Delay(20);
            SaveSettings();

            await _logger.Clear();
            int maxRowCount = 1000;

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
                QueryStringRels = SQLStringRels.Text,
                ConnectionString = SelectedQueryData.FormattedConnectionString,
                DBType = SelectedQueryData.ConnectionType,
                MaxRowCount = maxRowCount,
                UnpublishItems = UnpublishItems
            };

            await _qcHelper.UpdateSharpCloud(config, settings);

            SelectedQueryData.LogData = tbResults.Text;
            SelectedQueryData.LastRunDateTime = DateTime.Now;
            tbLastRun.Text = SelectedQueryData.LastRunDate;
            SaveSettings();

            UpdatingMessageVisibility = Visibility.Collapsed;
            await Task.Delay(20);
        }

        private bool TypeIsNumeric(Type type)
        {
            return type == typeof(double) || type == typeof(int) || type == typeof(float) || type == typeof(decimal) ||
                type == typeof(short) || type == typeof(long) || type == typeof(byte) || type == typeof(SByte) ||
                type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64);
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

        static void CopyResourceFile(string resFolder, string folder, string filename)
        {
            var appFolder = System.AppDomain.CurrentDomain.BaseDirectory;

            var sourceFileName = $"{appFolder}/{resFolder}/{filename}";
            var destFileName = $"{folder}/{filename}";

            File.Copy(sourceFileName, destFileName, true);
        }

        static void CopyResourceFileFromWeb(string folder, string filename)
        {
            var remote = string.Format("{0}/{1}", "https://sharpcloudonpremupdate.blob.core.windows.net:443/apidemos/sharpcloudSQLUpdate/SQLBatch6", filename);
            var local = string.Format("{0}/{1}", folder, filename);

            using (WebClient Client = new WebClient())
            {
                Client.DownloadFile(remote, local);
            }
        }

        private void NewConnectionClick(object sender, RoutedEventArgs e)
        {
            var newWnd = new SelectDatabaseType
            {
                Owner = this
            };

            if (newWnd.ShowDialog() == true)
            {
                _connections.Add(new QueryData(newWnd.SelectedButton));
                connectionList.SelectedIndex = _connections.Count - 1; // highlight the new item
                BrowserTabs.SelectedIndex = 0; // go back to the first tab
            }
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var qdNew = new QueryData(SelectedQueryData);
            _connections.Add(qdNew);
            connectionList.SelectedIndex = _connections.Count - 1; // highlight the new item
            BrowserTabs.SelectedIndex = 0; // go back to the  first tab
        }
        private void UpConnectionClick(object sender, RoutedEventArgs e)
        {
            int i = connectionList.SelectedIndex;
            if (i > 0)
            {
                i--;

                var selected = SelectedQueryData; // Keep reference to query data
                _connections.Remove(selected);
                _connections.Insert(i, selected);
                connectionList.SelectedIndex = i;
            }

        }
        private void DownConnectionClick(object sender, RoutedEventArgs e)
        {
            int i = connectionList.SelectedIndex;
            if (i < _connections.Count-1)
            {
                i++;

                var selected = SelectedQueryData; // Keep reference to query data
                _connections.Remove(selected);
                _connections.Insert(i, selected);
                connectionList.SelectedIndex = i;
            }
        }
        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            int i = connectionList.SelectedIndex;
            if (_connections.Count > 1)
            {
                _connections.Remove(SelectedQueryData);
                connectionList.SelectedIndex = i;
            }
        }

        private void connectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedQueryData != null)
            {
                ConnectionName.Text = SelectedQueryData.Name;
                ConnectionDescription.Text = SelectedQueryData.Description;
                txtDatabaseType.Text = SelectedQueryData.ConnectionType.ToString();
                ConnectionString.Text = SelectedQueryData.ConnectionsString;
                SQLString.Text = SelectedQueryData.QueryString;
                StoryId.Text = SelectedQueryData.StoryId;
                SQLStringRels.Text = SelectedQueryData.QueryStringRels;
                FileName.Text = SelectedQueryData.FileName;
                SharePointURL.Text = SelectedQueryData.SharePointURL;
                txtExampleRels.Text = "Example: " + SelectedQueryData.GetExampleRelQuery;
                SourceStoryId.Text = SelectedQueryData.SourceStoryId;

                tbLastRun.Text = SelectedQueryData.LastRunDate;
                tbResults.Text = SelectedQueryData.LogData;

                DataGrid.ItemsSource = SelectedQueryData.QueryResults;
                DataGridRels.ItemsSource = SelectedQueryData.QueryResultsRels;

                SetVisibleObjects(SelectedQueryData);
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

            // refresh the list
            var i = connectionList.SelectedIndex;
            connectionList.ItemsSource = null;
            connectionList.ItemsSource = _connections;
            connectionList.SelectedIndex = i;
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

        private void BrowseBut_Click(object sender, RoutedEventArgs e)
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
                else
                {
                    textBox.Background = (SolidColorBrush)Application.Current.Resources["QCBackground"];
                    textBox.Foreground = (SolidColorBrush) Application.Current.Resources["QCBlue"];
                }
            }
        }

        private void PublishSolutionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var nameError = _connectionNameValidator.Validate(SolutionNameTextBox.Text);
                var nameIsValid = string.IsNullOrWhiteSpace(nameError);

                if (!nameIsValid)
                {
                    MessageBox.Show(nameError);
                    return;
                }

                var sb = new StringBuilder();
                var sequenceFolder = GetFolder(SolutionNameTextBox.Text);

                var notEmpty = Directory.EnumerateFileSystemEntries(sequenceFolder).Any();
                if (notEmpty)
                {
                    var result = MessageBox.Show(
                        $"A folder named '{SolutionNameTextBox.Text}' already exist at this location, Do you want to replace?",
                        "WARNING",
                        MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                Directory.Delete(sequenceFolder, true);
                GetFolder(SolutionNameTextBox.Text);
                bool is32Bit;

                switch (ArchitectureComboBox.SelectedItem)
                {
                    case SolutionViewModel.Architecture64:
                        is32Bit = true;
                        break;

                    case SolutionViewModel.Architecture32:
                        is32Bit = false;
                        break;

                    default:
                        is32Bit = DetectIs32Bit;
                        break;
                }

                var connections = _solutionViewModel.IncludedConnections.Cast<QueryData>();
                sb.AppendLine("@echo off");

                foreach (var connection in connections)
                {
                    var path = GenerateBatchFile(
                        is32Bit,
                        connection.ConnectionsString,
                        SolutionNameTextBox.Text,
                        connection);

                    var suffix = GetFileSuffix(is32Bit);
                    var filename = $"SCSQLBatch{suffix}.exe";
                    sb.AppendLine($"echo Running: {connection.Name}");
                    sb.AppendLine($"\"{Path.Combine(path, filename)}\"");
                }

                var batchPath = Path.Combine(sequenceFolder, $"{SolutionNameTextBox.Text}.bat");
                var content = sb.ToString();
                File.WriteAllText(batchPath, content);
                Process.Start(sequenceFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorry, we were unable to complete the process\r\rError: {ex.Message}");
            }
        }
    }
}
