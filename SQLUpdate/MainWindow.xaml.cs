﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Windows;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using SC.API.ComInterop;
using System.Threading.Tasks;
using System.Windows.Threading;
using SC.API.ComInterop.Models;
using SCQueryConnect.Helpers;
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

        public SmartObservableCollection<QueryData> _connections = new SmartObservableCollection<QueryData>();

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
            cbDatabase.SelectedIndex = Int32.Parse(SaveHelper.RegRead("DBType", "0"));

            LoadAllProfiles();
            connectionList.ItemsSource = _connections;

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
                    // create one based on old settings - whcih are already set up
                    var pqd = CreateNewQueryData("Previous  Session");
                    pqd.StoryId = StoryId.Text;
                    _connections.Add(pqd);
                }

                // add some examples
                _connections.Add(CreateNewQueryData("SQL Server Example", 0, "Server=.; Integrated Security=true; Database=demo", "SELECT * FROM TABLE"));
                _connections.Add(CreateNewQueryData("ODBC Example", 1, "DSN=DatasourceName", "SELECT * FROM TABLE"));
                _connections.Add(CreateNewQueryData("MS Access Example", 2, "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\myFolder\\myAccessFile.accdb;", "SELECT * FROM TABLE"));
                _connections.Add(CreateNewQueryData("Excel Spreadsheet Example", 2, "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:/myFolder/mySpreadsheet.xlsx;Extended Properties=\"Excel 12.0 Xml; HDR = YES\";", "SELECT * from [Sheet1$]"));
            }

            connectionList.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveConnection", "0")));
            BrowserTabs.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveTab", "0")));
        }

        private QueryData CreateNewQueryData(string name, int dbType = -1, string connection = "", string query = "")
        {
            var qd = new QueryData(name)
            {
                ConnectionType = dbType == -1 ? cbDatabase.SelectedIndex : dbType,
                ConnectionsString = connection == "" ? ConnectionString.Text : connection,
                QueryString = query == "" ? SQLString.Text : query
            };
            return qd;
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.connectionstrings.com/");
        }

        private string _message = @"Connection strings allow you to connect to virtually any database.
Connection string may be very simple or quite detailed including username/password and servername, IP address etc.
Click YES to replace the current connection string text with the following sample text:

";
        private void ShowConnectString(string title, string connectstring)
        {
            string str = _message + connectstring;
            if (MessageBox.Show(str, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ConnectionString.Text = connectstring;
                ConnectionString.SelectAll();
                ConnectionString.Focus();
            }
        }
        
        private DbConnection GetDb(string connectionString)
        {
            switch (cbDatabase.SelectedIndex)
            {
                default:
                case 0:
                    return new SqlConnection(connectionString);
                case 1:
                    return new OdbcConnection(connectionString);
                case 2:
                    return new OleDbConnection(connectionString);
            }
        }

        private void TextConnectionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using (DbConnection connection = GetDb(ConnectionString.Text))
                {
                    connection.Open();
                    MessageBox.Show("Success!");
                    SaveSettings();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed! " + ex.Message);
            }
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
                if (heading == "ITEM1")
                    bOK1 = true;
                if (heading == "ITEM2")
                    bOK2 = true;
                if (heading == "ITEM 1")
                    bOK1 = true;
                if (heading == "ITEM 2")
                    bOK2 = true;
            }
            txterrRels.Visibility = (bOK1 && bOK2) ? Visibility.Collapsed : Visibility.Visible;

            return bOK1 && bOK2;
        }

        private void RunClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using (DbConnection connection = GetDb(ConnectionString.Text))
                {
                    connection.Open();
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = SQLString.Text;
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            CheckDataIsOK(reader);

                            DataTable dt = new DataTable();
                            dt.Load(reader);
                            DataGrid.ItemsSource = dt.DefaultView;

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
                using (DbConnection connection = GetDb(ConnectionString.Text))
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
                            DataGridRels.ItemsSource = dt.DefaultView;

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
            /*
            SaveHelper.RegWrite("ConnectionString", ConnectionString.Text);
            SaveHelper.RegWrite("SQLString", SQLString.Text);
            SaveHelper.RegWrite("StoryID", StoryId.Text);
            SaveHelper.RegWrite("DBType", cbDatabase.SelectedIndex.ToString());
            */

            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
            Directory.CreateDirectory(localPath);
            var s = SaveHelper.SerializeJSON(_connections);
            File.WriteAllText(localPath + "/connections.json", s);

            SaveHelper.RegWrite("ActiveConnection", connectionList.SelectedIndex.ToString());
            SaveHelper.RegWrite("ActiveTab", BrowserTabs.SelectedIndex.ToString());

        }

        private void SaveSettings(QueryData qd)
        {
            if (qd != null)
            {
                qd.Name = ConnectionName.Text;
                qd.ConnectionType = cbDatabase.SelectedIndex;
                qd.ConnectionsString = ConnectionString.Text;
                qd.QueryString = SQLString.Text;
                qd.StoryId = StoryId.Text;
                qd.QueryStringRels = SQLStringRels.Text;
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

            var username = Username.Text;
            var password = Password.Password;
            var url = Url.Text;
            var storyId = StoryId.Text;
            var connectionString = ConnectionString.Text;

            try
            {
                tbResults.Text += "Connecting to Sharpcloud " + url;
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                var sc = new SharpCloudApi(username, password, url);
                var story = sc.LoadStory(storyId);
                tbResults.Text += "\nReading story '" + story.Name + "'";
                tbResults.ScrollToEnd();
                var sqlString = SQLString.Text;
                await Task.Delay(20);

                using (DbConnection connection = GetDb(connectionString))
                {
                    connection.Open();
                    using (DbCommand command = connection.CreateCommand())
                    {
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
                                            data.Add($"{dbl:#.##}");
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

                            // create our string arrar
                            var arrayValues = new string[tempArray.Count + 1, reader.FieldCount];
                            // add the headers
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                arrayValues[0, i] = reader.GetName(i);
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
                                    Debug.Write(s+ '\t');
                                }
                                Debug.WriteLine("");
                                row++;
                            }

                            tbResults.Text += "\nProcessing " + row.ToString() + " rows";
                            tbResults.ScrollToEnd();
                            await Task.Delay(20);

                            // pass the array to SharpCloud
                            string errorMessage;
                            if (story.UpdateStoryWithArray(arrayValues, false, out errorMessage))
                            {
                                if (story.IsModified)
                                {
                                    tbResults.Text += "\nSaving Changes ";
                                    await Task.Delay(20);
                                    tbResults.ScrollToEnd();
                                    story.Save();
                                    tbResults.Text += "\nSave Complete!";
                                    tbResults.ScrollToEnd();
                                    await Task.Delay(20);
                                }
                                else
                                {
                                    tbResults.Text += "\nNo Changes detected";
                                    tbResults.ScrollToEnd();
                                    await Task.Delay(20);
                                }
                            }
                            else
                            {
                                MessageBox.Show(errorMessage);
                                tbResults.ScrollToEnd();
                                tbResults.Text += "\nERROR: " + errorMessage;
                            }
                        }
                    } 
                    //UpdateRels(connection, story);
                }
                UpdatingMessageVisibility = Visibility.Collapsed;
                await Task.Delay(20);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
                tbResults.Text += "\nERROR: " + ex.Message;
                UpdatingMessageVisibility = Visibility.Collapsed;
            }
        }

        private async void UpdateItems(DbConnection connection, Story story)
        {
            var sqlString = SQLString.Text;

            if (string.IsNullOrWhiteSpace(sqlString))
            {
                tbResults.Text += "\nNo script for Items - Ignoring this step";
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                return;
            }
        }

        private void GenerateBatchFile(object sender, RoutedEventArgs e)
        {
            if (!ValidateCreds())
                return;

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
            folder += "/data";
            Directory.CreateDirectory(folder);

            var qd = connectionList.SelectedItem as QueryData;
            folder += "/" + qd.Name;
            Directory.CreateDirectory(folder);

            try
            {
                var configFilename = folder + "/SCSQLBatch.exe.config";

                if (File.Exists(configFilename))
                {
                    if (MessageBox.Show("Config files alredy exist in this location, Do you want to replace?", "WARNING", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        return;
                }

                try
                {
                    CopyResourceFile(folder, "SC.API.ComInterop.dll");
                    CopyResourceFile(folder, "SC.Api.dll");
                    CopyResourceFile(folder, "SCSQLBatch.exe");
                    CopyResourceFile(folder, "SCSQLBatch.exe.config");
                }
                catch (Exception exception2)
                {
                    MessageBox.Show("Sorry, we were unable to complete the process as we could not downlaod the files from the internet. Please make sure you have a working internet connection. " + exception2.Message, "NO CONNECTION");
                    return;    
                }

                string [] dbTypes = { "SQL", "ODBC", "OLDDB"};
                // set up the config
                var content = File.ReadAllText(configFilename);
                content = content.Replace("USERID", Username.Text);
                content = content.Replace("PASSWORD", ""); // we keep the password hidden
                content = content.Replace("BASE64PWORD", Convert.ToBase64String(Encoding.Default.GetBytes(Password.Password)));
                content = content.Replace("https://my.sharpcloud.com", Url.Text);
                content = content.Replace("00000000-0000-0000-0000-000000000000", StoryId.Text);
                if (cbDatabase.SelectedIndex != -1)
                    content = content.Replace("SQL", dbTypes[cbDatabase.SelectedIndex]);
                content = content.Replace("CONNECTIONSTRING", ConnectionString.Text.Replace("\r", " ").Replace("\n", " "));
                content = content.Replace("QUERYSTRING", SQLString.Text.Replace("\r", " ").Replace("\n", " "));
                File.WriteAllText(configFilename, content);

                Process.Start(folder);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
        
        static void CopyResourceFile(string folder, string filename)
        {
            var remote = string.Format("{0}/{1}", "https://sharpcloudonpremupdate.blob.core.windows.net:443/apidemos/sharpcloudSQLUpdate/SQLBatch", filename);
            var local = string.Format("{0}/{1}", folder, filename);

            WebClient Client = new WebClient();
            Client.DownloadFile(remote, local);
        }

        private void NewConnectionClick(object sender, RoutedEventArgs e)
        {
            var qd = connectionList.SelectedItem as QueryData;
            _connections.Add(CreateNewQueryData(qd.Name + " Copy"));
            connectionList.SelectedIndex = _connections.Count - 1; // highlight the new item
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
                cbDatabase.SelectedIndex = qd.ConnectionType;
                ConnectionString.Text = qd.ConnectionsString;
                SQLString.Text = qd.QueryString;
                StoryId.Text = qd.StoryId;
                SQLStringRels.Text = qd.QueryStringRels;
            }
        }

        private void ConnectionString_LostFocus(object sender, RoutedEventArgs e)
        {
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
    }
}
