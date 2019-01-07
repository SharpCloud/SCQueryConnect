using Microsoft.Win32;
using SC.API.ComInterop;
using SC.API.ComInterop.ArrayProcessing;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common;
using SCQueryConnect.Helpers;
using SCQueryConnect.ViewModels;
using SCQueryConnect.Views;
using SQLUpdate.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
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
        private readonly string[] _validItem1Headings =
        {
            "ITEM 1",
            "EXTERNALID1",
            "EXTERNALID 1",
            "EXTERNAL ID 1",
            "INTERNAL ID 1"
        };

        private readonly string[] _validItem2Headings =
{
            "ITEM 2",
            "EXTERNALID2",
            "EXTERNALID 2",
            "EXTERNAL ID 2",
            "INTERNAL ID 2"
        };

        public string AppNameOnly => $"SharpCloud QueryConnect v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public string AppName
        {
            get
            {
                if (IntPtr.Size == 4)
                    return $"{AppNameOnly} - 32Bit(x86)";
                return $"{AppNameOnly} - 64Bit(AnyCPU)";
            }
        }


        public Visibility UpdatingMessageVisibility
        {
            get { return _updatingMessageVisibility; }
            set
            {
                _updatingMessageVisibility = value;
                OnPropertyChanged("UpdatingMessageVisibility");
            }
        }

        private Visibility _updatingMessageVisibility = Visibility.Collapsed;

        public bool UnpublishItems
        {
            get { return _unpublishItems; }
            set
            {
                _unpublishItems = value;
                OnPropertyChanged("UnpublishItems");
            }
        }
        private bool _unpublishItems = false;

        public SmartObservableCollection<QueryData> _connections = new SmartObservableCollection<QueryData>();

        private ProxyViewModel _proxyViewModel;
        private QueryConnectHelper _qcHelper = new QueryConnectHelper();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContext = this;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionString.Text = SaveHelper.RegRead("ConnectionString",
                "Server=.; Integrated Security=true; Database=demo");
            SQLString.Text = SaveHelper.RegRead("SQLString", "SELECT * FROM TABLE");
            Url.Text = SaveHelper.RegRead("URL", "https://my.sharpcloud.com");
            Username.Text = SaveHelper.RegRead("Username", "");
            Password.Password = Encoding.Default.GetString(Convert.FromBase64String(SaveHelper.RegRead("Password", "")));
            StoryId.Text = SaveHelper.RegRead("StoryID", "");

            _proxyViewModel = new ProxyViewModel();
            _proxyViewModel.Proxy = SaveHelper.RegRead("Proxy", "");
            _proxyViewModel.ProxyAnnonymous = bool.Parse(SaveHelper.RegRead("ProxyAnonymous", "true"));
            _proxyViewModel.ProxyUserName = SaveHelper.RegRead("ProxyUserName", "");
            _proxyViewModel.ProxyPassword = Encoding.Default.GetString(Convert.FromBase64String(SaveHelper.RegRead("ProxyPassword", "")));

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

            var splashScreen = new SplashScreen("Images/splash.jpg");
            splashScreen.Show(true);

        }

        private void LoadAllProfiles()
        {
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
            var file = localPath + "/connections.json";

            if (File.Exists(file))
            {
                // load our previous settings
                _connections = SaveHelper.DeserializeJSON<SmartObservableCollection<QueryData>>(File.ReadAllText(file));
            }
            else
            {   // create some sample settings
                if (SQLString.Text != "SELECT * FROM TABLE" && ConnectionString.Text != "Server=.; Integrated Security=true; Database=demo")
                {
                    // user papbably already has setting we should save from last time
                    // create one based on old settings - which are already set up
                    var pqd = new QueryData((QueryData.DbType)Int32.Parse(SaveHelper.RegRead("DBType", "0")));
                    pqd.Name = "Previous Connection";
                    pqd.ConnectionsString = SaveHelper.RegRead("ConnectionString", "Server=.; Integrated Security=true; Database=demo");
                    pqd.QueryString = SaveHelper.RegRead("SQLString", "SELECT * FROM TABLE");
                    pqd.StoryId = StoryId.Text;
                    _connections.Add(pqd);
                }

                // add some examples
                _connections.Add(new QueryData(QueryData.DbType.Excel)); 
                _connections.Add(new QueryData(QueryData.DbType.Access));
                _connections.Add(new QueryData(QueryData.DbType.SharepointList));
                _connections.Add(new QueryData(QueryData.DbType.SQL));
                _connections.Add(new QueryData(QueryData.DbType.ODBC));
                _connections.Add(new QueryData(QueryData.DbType.ADO));
            }
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.connectionstrings.com/");
        }

        private DbConnection GetDb()
        {
            return (connectionList.SelectedItem as QueryData).GetDb();
        }

        private void TestConnectionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using (DbConnection connection = GetDb())
                {
                    connection.Open();
                    MessageBox.Show("Hooray! It looks like it's worked!");
                    SaveSettings();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("It did not work! " + ex.Message);
            }
        }

        private void ReviewConnectionClick(object sender, RoutedEventArgs e)
        {
            var qd = connectionList.SelectedItem as QueryData;
            string info = string.Format("Internal Connection Type:\n{0}\n\nConnection String:\n{1}", qd.GetBatchDBType,
                qd.FormattedConnectionString);
            var dlg = new ConnectionInfo(info);
            dlg.ShowDialog();
            //MessageBox.Show(info, "Internal Connection Info");
            }

        private bool CheckDataIsOK(DbDataReader reader)
        {
            bool bOK = false;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var heading = reader.GetName(i).ToUpper();
                if (heading == "NAME")
                    bOK = true;
                else if (heading == "EXTERNAL ID")
                    bOK = true;
                else if (heading == "EXTERNALID")
                    bOK = true;
            }
            txterr.Visibility = bOK ? Visibility.Collapsed : Visibility.Visible;

            return bOK;
        }

        private bool CheckDataIsOKRels(DbDataReader reader)
        {
            bool bOK1 = false;
            bool bOK2 = false;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var heading = reader.GetName(i).ToUpper();

                if (_validItem1Headings.Contains(heading))
                    bOK1 = true;

                if (_validItem2Headings.Contains(heading))
                    bOK2 = true;
            }
            txterrRels.Visibility = (bOK1 && bOK2) ? Visibility.Collapsed : Visibility.Visible;

            return bOK1 && bOK2;
        }

        private void RunClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using (DbConnection connection = GetDb())
                {
                    connection.Open();
                    using (DbCommand command = connection.CreateCommand())
                    {

                        if (connection is OleDbConnection) { 
                            DataTable schemaTable = ((OleDbConnection)connection).GetOleDbSchemaTable(
                                OleDbSchemaGuid.Tables,
                                new object[] {null, null, null, "TABLE"});

                            Debug.Write( schemaTable.ToString());
                        }



                        command.CommandText = SQLString.Text;
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            CheckDataIsOK(reader);

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
                            (connectionList.SelectedItem as QueryData).QueryResults = dt.DefaultView;

                            for (var col = 0; col < DataGrid.Columns.Count; col++)
                            {
                                DataGrid.Columns[col].Header = dt.Columns[col].Caption;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error: " + ex.Message);
            }
        }

        private void RunClickRels(object sender, RoutedEventArgs e)
        {
            try
            {
                using (DbConnection connection = GetDb())
                {
                    connection.Open();
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = SQLStringRels.Text;
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            CheckDataIsOKRels(reader);

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
                            (connectionList.SelectedItem as QueryData).QueryResultsRels = dt.DefaultView;

                            for (var col = 0; col < DataGridRels.Columns.Count; col++)
                            {
                                DataGridRels.Columns[col].Header = dt.Columns[col].Caption;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error: " + ex.Message);
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
            SaveHelper.RegWrite("Password", Convert.ToBase64String(Encoding.Default.GetBytes(Password.Password)));

            SaveSettings(connectionList.SelectedItem as QueryData);

            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
            Directory.CreateDirectory(localPath);
            var s = SaveHelper.SerializeJSON(_connections);
            File.WriteAllText(localPath + "/connections.json", s);

            SaveHelper.RegWrite("ActiveConnection", connectionList.SelectedIndex.ToString());
            SaveHelper.RegWrite("ActiveTab", BrowserTabs.SelectedIndex.ToString());

            SaveHelper.RegWrite("UnpublishItems", UnpublishItems.ToString());

            SaveHelper.RegWrite("Proxy", _proxyViewModel.Proxy);
            SaveHelper.RegWrite("ProxyAnonymous", _proxyViewModel.ProxyAnnonymous);
            SaveHelper.RegWrite("ProxyUserName", _proxyViewModel.ProxyUserName);
            SaveHelper.RegWrite("ProxyPassword", Convert.ToBase64String(Encoding.Default.GetBytes(_proxyViewModel.ProxyPassword)));
        }

        private void SaveSettings(QueryData qd)
        {
            if (qd != null)
            {
                qd.Name = ConnectionName.Text;
                qd.Description = ConnectionDescription.Text;
                qd.ConnectionsString = ConnectionString.Text;
                qd.QueryString = SQLString.Text;
                qd.StoryId = StoryId.Text;
                qd.QueryStringRels = SQLStringRels.Text;
                qd.FileName = FileName.Text;
                qd.SharePointURL = SharePointURL.Text;
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

            tbResults.Text = ""; // clear
            tbResults.ScrollToEnd();
            await Task.Delay(20);

            var username = Username.Text;
            var password = Password.Password;
            var url = Url.Text;
            var storyId = StoryId.Text;
            var qd = connectionList.SelectedItem as QueryData;

            try
            {
                var start = DateTime.Now;
                tbResults.Text += $"Starting update process at {DateTime.Now:dd MMM yyyy HH:mm:ss}\n";
                tbResults.Text += "Connecting to Sharpcloud " + url;
                tbResults.ScrollToEnd();
                await Task.Delay(20);

                var sc = new SharpCloudApi(username, password, url, _proxyViewModel.Proxy, _proxyViewModel.ProxyAnnonymous, _proxyViewModel.ProxyUserName, _proxyViewModel.ProxyPassword);
                var story = sc.LoadStory(storyId);
                var isValid = _qcHelper.Validate(story, out var message);
                tbResults.Text += $"\n{message}";
                tbResults.ScrollToEnd();
                await Task.Delay(20);

                if (isValid)
                {
                    using (DbConnection connection = GetDb())
                    {
                        connection.Open();
                        await UpdateItems(connection, story, SQLString.Text);
                        await UpdateRelationships(connection, story, SQLStringRels.Text);

                        tbResults.Text += "\nSaving Changes ";
                        await Task.Delay(20);
                        tbResults.ScrollToEnd();
                        story.Save();
                        tbResults.Text += "\nSave Complete!";
                        tbResults.ScrollToEnd();
                        await Task.Delay(20);

                        tbResults.Text += $"\nUpdate process completed in {(DateTime.Now - start).TotalSeconds:f2} seconds";

                        qd.LogData = tbResults.Text;
                        qd.LastRunDateTime = DateTime.Now;
                        tbLastRun.Text = qd.LastRunDate;
                        SaveSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                tbResults.Text += "\nError: " + ex.Message;
                await Task.Delay(20);
                tbResults.ScrollToEnd();
            }

            UpdatingMessageVisibility = Visibility.Collapsed;
            await Task.Delay(20);
        }

        private async Task UpdateItems(DbConnection connection, Story story, string sqlString)
        {
            if (string.IsNullOrWhiteSpace(sqlString))
            {
                tbResults.Text += "\nNo Item Query detected";
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                return;
            }

            using (DbCommand command = connection.CreateCommand())
            {
                int MaxRowCount = 1000;
                try
                {
                    MaxRowCount = Int32.Parse(ConfigurationManager.AppSettings["MaxRowCount"]);
                }
                catch (Exception E)
                {
                    MaxRowCount = 1000;
                }

                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                tbResults.Text += "\nReading database";
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                using (DbDataReader reader = command.ExecuteReader())
                {
                    CheckDataIsOK(reader);

                    var tempArray = new List<List<string>>();
                    while (reader.Read())
                    {
                        var objs = new object[reader.FieldCount];
                        reader.GetValues(objs);
                        var data = new List<string>();
                        foreach (var o in objs)
                        {
                            if (o is DateTime?)
                            {
                                // definately date time
                                var date = (DateTime) o;
                                data.Add(date.ToString("yyyy MM dd"));
                            }
                            else
                            {
                                DateTime date;
                                double dbl;
                                var s = o.ToString();
                                if (double.TryParse(s, out dbl))
                                {
                                    data.Add($"{dbl:0.##}");
                                }
                                else if (DateTime.TryParse(s, out date))
                                {
                                    data.Add(date.ToString("yyyy MM dd"));
                                }
                                else if (s.ToLower().Trim() == "null")
                                {
                                    data.Add("");
                                }
                                else
                                {
                                    data.Add(s);
                                }
                            }
                        }
                        tempArray.Add(data);
                    }

                    if (tempArray.Count > MaxRowCount)
                    {
                        var s = $"Your item query contains too many records (more than {MaxRowCount}). Updating large data sets into SharpCloud may result in stories that are too big to load or have poor performance. Please try refining you query by adding a WHERE clause.";
                        MessageBox.Show(s);
                        tbResults.Text += "\n" + s;
                        tbResults.ScrollToEnd();
                        await Task.Delay(20);
                        return;
                    }

                    // create our string arrar
                    var arrayValues = new string[tempArray.Count + 1, reader.FieldCount];
                    // add the headers
                    var regex = new Regex(Regex.Escape("#"));
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var header = reader.GetName(i);
                        if (header.ToLower().StartsWith("tags#"))
                        {
                            header = regex.Replace(header, ".", 1);
                        }
                        arrayValues[0, i] = header;
                            
                        Debug.Write(arrayValues[0, i] + '\t');
                    }
                    // add the data values
                    int row = 1;
                    foreach (var list in tempArray)
                    {
                        int col = 0;
                        foreach (string s in list)
                        {
                            arrayValues[row, col++] = s;
                            Debug.Write(s + '\t');
                        }
                        Debug.WriteLine("");
                        row++;
                    }

                    tbResults.Text += "\nProcessing " + row.ToString() + " rows";
                    tbResults.ScrollToEnd();
                    await Task.Delay(20);

                    // pass the array to SharpCloud
                    string errorMessage;
                    if (UnpublishItems)
                    {
                        List<Guid> updatedItems;
                        if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage, out updatedItems))
                        {
                            MessageBox.Show(errorMessage);
                            tbResults.ScrollToEnd();
                            tbResults.Text += errorMessage;
                        } else
                        {
                            foreach (var item in story.Items)
                            {
                                if (!updatedItems.Contains(item.AsElement.ID))
                                {
                                    try
                                    {
                                        item.IsPublished = false;
                                    }
                                    catch (FieldAccessException ex)
                                    {
                                        errorMessage += "\nERROR: " + ex.Message;
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                tbResults.ScrollToEnd();
                                tbResults.Text += errorMessage;
                            }
                        }
                    }
                    else {
                        if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage))
                        {
                            MessageBox.Show(errorMessage);
                            tbResults.ScrollToEnd();
                            tbResults.Text += errorMessage;
                        } else
                        {
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                tbResults.ScrollToEnd();
                                tbResults.Text += errorMessage;
                            }
                        }
                    }
                }
            }
        }

        private bool TypeIsNumeric(Type type)
        {
            return type == typeof(double) || type == typeof(int) || type == typeof(float) || type == typeof(decimal) ||
                type == typeof(short) || type == typeof(long) || type == typeof(byte) || type == typeof(SByte) ||
                type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64);
        }

        private async Task UpdateRelationships(DbConnection connection, Story story, string sqlString)
        {
            if (string.IsNullOrWhiteSpace(sqlString))
            {
                tbResults.Text += "\nNo Relationship Query detected";
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                return;
            }

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                tbResults.Text += "\nReading database for relationships";
                tbResults.ScrollToEnd();
                await Task.Delay(20);

                int columnCount;
                var dataList = new List<string[]>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (!CheckDataIsOKRels(reader))
                    {
                        tbResults.Text += "\nERROR: Invalid SQL";
                        return;
                    }

                    // Write array column headers

                    columnCount = reader.FieldCount;
                    for (int i = 0; i < columnCount; i++)
                    {
                        dataList.Add(new string[columnCount]);
                        dataList[0][i] = reader.GetName(i);
                    }

                    // Write array data

                    while (reader.Read())
                    {
                        var dataRow = new string[columnCount];
                        dataList.Add(dataRow);

                        for (int i = 0; i < columnCount; i++)
                        {
                            dataRow[i] = reader[i].ToString();
                        }
                    }
                }

                // Create data array to pass to SharpCloud

                var filteredData = dataList.Where(strArray =>
                    strArray.Any(s => !string.IsNullOrWhiteSpace(s)))
                    .ToList();

                var data = new string[filteredData.Count, columnCount];

                for (int row = 0; row < filteredData.Count; row++)
                {
                    for (int column = 0; column < columnCount; column++)
                    {
                        data[row, column] = filteredData[row][column];
                    }
                }

                var updater = new RelationshipsUpdater();
                updater.UpdateRelationships(data, story);
            }
        }

        private void ViewExisting(object sender, RoutedEventArgs e)
        {
            Process.Start(GetFolder());
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
            GenerateBatchFile((IntPtr.Size == 4));
        }

        private string GetFolder()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
            folder += "/data";
            Directory.CreateDirectory(folder);
            var qd = connectionList.SelectedItem as QueryData;
            folder += "/" + qd.Name;
            Directory.CreateDirectory(folder);

            return folder;
        }

        private void GenerateBatchFile(bool b32bit)
        {
            var suffix = b32bit ? "x86" : string.Empty;
            string zipfile = $"SCSQLBatch{suffix}.zip";

            if (!ValidateCreds())
                return;

            var folder = GetFolder();
            var qd = connectionList.SelectedItem as QueryData;

            try
            {
                var configFilename = folder + $"/SCSQLBatch{suffix}.exe.config";

                if (File.Exists(configFilename))
                {
                    if (MessageBox.Show("Config files alredy exist in this location, Do you want to replace?", "WARNING", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        return;
                }

                if (ConnectionString.Text.Contains("\""))
                    MessageBox.Show(
                        "Your connection string and/or query string contains '\"', which will automatically be replaced with '");
                try
                {
                    File.Delete($"{folder}/Newtonsoft.Json.dll");
                    File.Delete($"{folder}/SC.Framework.dll");
                    File.Delete($"{folder}/SC.API.ComInterop.dll");
                    File.Delete($"{folder}/SC.Api.dll");
                    File.Delete($"{folder}/SC.SharedModels.dll");
                    File.Delete($"{folder}/SCSQLBatch{suffix}.exe");
                    File.Delete($"{folder}/SCSQLBatch{suffix}.exe.config");
                    File.Delete($"{folder}/SCSQLBatch.zip");
                    File.Delete($"{folder}/SCQueryConnect.Common.dll");

                    ZipFile.ExtractToDirectory(zipfile, folder);
                }
                catch (Exception exception2)
                {
                    MessageBox.Show($"Sorry, we were unable to complete the process\r\rError: {exception2.Message}");
//                    MessageBox.Show("Sorry, we were unable to complete the process as we could not downlaod the files from the internet. Please make sure you have a working internet connection. " + exception2.Message, "NO CONNECTION");
                    return;    
                }

                // set up the config
                var content = File.ReadAllText(configFilename);
                content = content.Replace("USERID", Username.Text);
                content = content.Replace("PASSWORD", ""); // we keep the password hidden
                content = content.Replace("BASE64PWORD", Convert.ToBase64String(Encoding.Default.GetBytes(Password.Password)));
                content = content.Replace("https://my.sharpcloud.com", Url.Text);
                content = content.Replace("00000000-0000-0000-0000-000000000000", StoryId.Text);
                content = content.Replace("SQL", qd.GetBatchDBType);
                content = content.Replace("CONNECTIONSTRING", qd.FormattedConnectionString.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = content.Replace("QUERYSTRING", qd.QueryString.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = content.Replace("QUERYRELSSTRING", qd.QueryStringRels.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = content.Replace("LOGFILE", $"Logfile.txt");
                content = content.Replace("UNPUBLISHITEMS", UnpublishItems.ToString());
                content = content.Replace("PROXYADDRESS", _proxyViewModel.Proxy);
                content = content.Replace("PROXYANONYMOUS", _proxyViewModel.ProxyAnnonymous.ToString());
                content = content.Replace("PROXYUSERNAME", _proxyViewModel.ProxyUserName);
                content = content.Replace("PROXYPWORD", "");
                content = content.Replace("BASE64PROXYPWRD", Convert.ToBase64String(Encoding.Default.GetBytes(_proxyViewModel.Proxy)));

                File.WriteAllText(configFilename, content);

                // update the Logfile
                var logfile = $"{folder}Logfile.txt";
                var contentNotes = new List<string>();
                contentNotes.Add($"----------------------------------------------------------------------");
                contentNotes.Add(b32bit
                    ? $"32 bit (x86) Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}"
                    : $"64 bit Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}");
                contentNotes.Add($"----------------------------------------------------------------------");

                File.AppendAllLines(logfile, contentNotes);

                Process.Start(folder);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
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
            var newWnd = new SelectDatabaseType();
            if (newWnd.ShowDialog() == true)
            {
                _connections.Add(new QueryData(newWnd.SelectedButton));
                connectionList.SelectedIndex = _connections.Count - 1; // highlight the new item
                BrowserTabs.SelectedIndex = 0; // go back to the first tab
            }
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var qd = connectionList.SelectedItem as QueryData;
            var qdNew = new QueryData(qd);
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
                var qd = connectionList.SelectedItem as QueryData;
                _connections.Remove(qd);
                _connections.Insert(i, qd);
                connectionList.SelectedIndex = i;
            }

        }
        private void DownConnectionClick(object sender, RoutedEventArgs e)
        {
            int i = connectionList.SelectedIndex;
            if (i < _connections.Count-1)
            {
                i++;
                var qd = connectionList.SelectedItem as QueryData;
                _connections.Remove(qd);
                _connections.Insert(i, qd);
                connectionList.SelectedIndex = i;
            }
        }
        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            int i = connectionList.SelectedIndex;
            if (_connections.Count > 1)
            {
                var qd = connectionList.SelectedItem as QueryData;
                _connections.Remove(qd);
                connectionList.SelectedIndex = i;
            }
        }

        private void connectionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var qd = connectionList.SelectedItem as QueryData;
            if (qd != null)
            {
                ConnectionName.Text = qd.Name;
                ConnectionDescription.Text = qd.Description;
                txtDatabaseType.Text = qd.ConnectionType.ToString();
                ConnectionString.Text = qd.ConnectionsString;
                SQLString.Text = qd.QueryString;
                StoryId.Text = qd.StoryId;
                SQLStringRels.Text = qd.QueryStringRels;
                FileName.Text = qd.FileName;
                SharePointURL.Text = qd.SharePointURL;
                txtExampleRels.Text = "Example: " + qd.GetExampleRelQuery;

                tbLastRun.Text = qd.LastRunDate;
                tbResults.Text = qd.LogData;

                DataGrid.ItemsSource = qd.QueryResults;
                DataGridRels.ItemsSource = qd.QueryResultsRels;

                SetVisibeObjects(qd);
            }
        }

        private void ConnectionString_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void LostFocusStoryID(object sender, RoutedEventArgs e)
        {
            var s = StoryId.Text;
            StoryId.Text = _qcHelper.GetStoryUrl(s);
            SaveSettings();
        }

        private void FileName_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void Sharepoint_LostFocus(object sender, RoutedEventArgs e)
        {
            // note - the list ID can be found by reading data from the following url
            // SharePointURL/_api/web/lists
            // TODO give the user a way of seacrching for the list ID..
            // currently this can easily be done from Excel.

            var qd = connectionList.SelectedItem as QueryData;
            var text = SharePointURL.Text;
            if (text.ToUpper().Contains("LIST="))
            {
                // user has provided a good link with a specified list.
                // make sure the list is a guid 
                text = text.Replace("%7B", "{").Replace("%2D", "-").Replace("%7D", "}");
            }
            qd.SharePointURL = text;
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

        private void SetVisibeObjects(QueryData qd)
        {
            if (qd != null)
            {
                var v1 = Visibility.Visible;
                var v2 = Visibility.Collapsed;
                var v3 = Visibility.Collapsed;

                switch (qd.ConnectionType)
                {
                    case QueryData.DbType.Access: 
                    case QueryData.DbType.Excel: 
                        v1 = Visibility.Collapsed;
                        v2 = Visibility.Visible;
                        v3 = Visibility.Collapsed;
                        break;
                    case QueryData.DbType.SharepointList:
                        v1 = Visibility.Collapsed;
                        v2 = Visibility.Collapsed;
                        v3 = Visibility.Visible;
                        break;
                }
                lbl1.Visibility = v1;
                ConnectionString.Visibility = v1;
                lbl2.Visibility = v2;
                FileName.Visibility = v2;
                BrowseBut.Visibility = v2;
                lbl3.Visibility = v3;
                SharePointURL.Visibility = v3;
            }
        }

        private void BrowseBut_Click(object sender, RoutedEventArgs e)
        {
            var qd = connectionList.SelectedItem as QueryData;
            var ord = new OpenFileDialog();
            ord.Filter = qd.ConnectionType == QueryData.DbType.Excel ? "Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx" : "Access Database Files (*.accdb;*.mdb)|*.accdb;*.mdb";

            if (ord.ShowDialog() == true)
            {
                qd.FileName = ord.FileName;
                FileName.Text = ord.FileName;
            }
        }
        
        private SharpCloudApi GetApi()
        {
            return new SharpCloudApi(Username.Text, Password.Password, Url.Text, _proxyViewModel.Proxy, _proxyViewModel.ProxyAnnonymous, _proxyViewModel.ProxyUserName, _proxyViewModel.ProxyPassword);
        }


        private void SelectStoryClick(object sender, RoutedEventArgs e)
        {
            var sel = new SelectStory(GetApi(), false, Username.Text);

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

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start($"https://github.com/SharpCloud/SCQueryConnect");
        }

        private void Hyperlink_Click2(object sender, RoutedEventArgs e)
        {
            Process.Start($"https://www.microsoft.com/en-gb/download/details.aspx?id=13255");
        }

        private void Hyperlink_Click3(object sender, RoutedEventArgs e)
        {
            Process.Start($"https://www.youtube.com/watch?v=cZUyQkVzg2E");
        }

        private void Hyperlink_Click4(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", System.AppDomain.CurrentDomain.BaseDirectory);
        }
    
        private void Proxy_OnClick(object sender, RoutedEventArgs e)
        {
            var proxy = new ProxySettings(_proxyViewModel);
            proxy.ShowDialog();
        }
    }
}
