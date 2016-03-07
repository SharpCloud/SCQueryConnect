using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using SC.API.ComInterop;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Resources;
using System.Windows.Threading;
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

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            DataContext = this;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionString.Text = RegRead("ConnectionString", "Server=.; Integrated Security=true; Database=demo");
            SQLString.Text = RegRead("SQLString", "SELECT * FROM TABLE");

            Url.Text = RegRead("URL", "https://my.sharpcloud.com");
            Username.Text = RegRead("Username", "");
            Password.Password = Encoding.Default.GetString(Convert.FromBase64String(RegRead("Password", "")));
            StoryId.Text = RegRead("StoryID", "");
            cbDatabase.SelectedIndex = Int32.Parse(RegRead("DBType", "0"));
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.connectionstrings.com/");
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

        private void EgButtonClick(object sender, RoutedEventArgs e)
        {
            switch (cbDatabase.SelectedIndex)
            {
                case 0:
                    ShowConnectString("SQL example", "Server=.; Integrated Security=true; Database=demo");
                    break;
                case 1:
                    ShowConnectString("ODBC example", "DSN=DatasourceName");
                    break;
                case 2:
                    ShowConnectString("ADO example", "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\myFolder\\myAccessFile.accdb;");
                    break;
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
            txterr.Visibility = bOK? Visibility.Collapsed : Visibility.Visible;

            return bOK;
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

        const string RegKey = "SOFTWARE\\SharpCloud\\SQLUpdate";

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

        public static string RegRead(string KeyName, string defVal)
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(RegKey);
            // If the RegistrySubKey doesn't exist -> (null)
            {
                try
                {
                    // If the RegistryKey exists I get its value
                    // or null is returned.
                    if (sk1 != null)
                    {
                        var ret = (string)sk1.GetValue(KeyName.ToUpper());
                        if (ret == null)
                            return defVal;
                        return ret;
                    }
                }
                catch (Exception e)
                {
                    // AAAAAAAAAAARGH, an error!
                    //ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
                }
            }
            return defVal;
        }

        public static bool RegWrite(string KeyName, object Value)
        {
            try
            {
                // Setting
                RegistryKey rk = Registry.CurrentUser;
                // I have to use CreateSubKey 
                // (create or open it if already exits), 
                // 'cause OpenSubKey open a subKey as read-only
                RegistryKey sk1 = rk.CreateSubKey(RegKey);
                // Save the value
                sk1.SetValue(KeyName.ToUpper(), Value);

                return true;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                //ShowErrorMessage(e, "Writing registry " + KeyName.ToUpper());
                return false;
            }
        }


        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            RegWrite("ConnectionString", ConnectionString.Text);
            RegWrite("SQLString", SQLString.Text);

            RegWrite("URL", Url.Text);
            RegWrite("Username", Username.Text);
            RegWrite("Password", Convert.ToBase64String(Encoding.Default.GetBytes(Password.Password)));
            RegWrite("StoryID", StoryId.Text);
            RegWrite("DBType", cbDatabase.SelectedIndex.ToString());
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
            var sqlString = SQLString.Text;

            try
            {
                tbResults.Text += "Connecting to Sharpcloud " + url;
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                var sc = new SharpCloudApi(username, password, url);
                var story = sc.LoadStory(storyId);
                tbResults.Text += "\nReading story '" + story.Name + "'";
                tbResults.ScrollToEnd();
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
                                        var date = (DateTime)o;
                                        data.Add(date.ToString("yyyy MM dd"));
                                    }
                                    else
                                    {
                                        data.Add(o.ToString());
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
                            }
                            // add the data values
                            int row = 1;
                            foreach (var list in tempArray)
                            {
                                int col = 0;
                                foreach (string s in list)
                                {
                                    arrayValues[row, col++] = s;
                                }
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

        private void GenerateBatchFile(object sender, RoutedEventArgs e)
        {
            if (!ValidateCreds())
                return;

            var dlg = new FolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Description = "Select or create a folder to store the batch files in"
            };
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var folder = dlg.SelectedPath;

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

                    MessageBox.Show(@"The files to repeat the database update to the SharpCloud story have been saved in this folder.

You can run the file SCSQLBatch.exe to run the update at any time.

For repeated scheduled updates, use a process like 'Task Scheduler' to make updates every day, hour etc.
", "INFORMATION");
                    Process.Start(folder);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }
        
        static void CopyResourceFile(string folder, string filename)
        {
            var remote = string.Format("{0}/{1}", "https://sharpcloudonpremupdate.blob.core.windows.net:443/apidemos/sharpcloudSQLUpdate/SQLBatch", filename);
            var local = string.Format("{0}/{1}", folder, filename);

            WebClient Client = new WebClient();
            Client.DownloadFile(remote, local);
        }
    }
}
