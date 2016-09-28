using System;
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
using System.Linq;
using System.Net;
using SC.API.ComInterop;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using mshtml;
using Microsoft.Win32;
using SC.API.ComInterop.Models;
using SCQueryConnect.Helpers;
using SCQueryConnect.Views;
using SQLUpdate.Views;
using Directory = System.IO.Directory;
using MessageBox = System.Windows.MessageBox;
using System.Globalization;

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

            //cbDatabase.SelectedIndex = Int32.Parse(SaveHelper.RegRead("DBType", "0"));

            LoadAllProfiles();
            connectionList.ItemsSource = _connections;

            // choose our last settings
            connectionList.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveConnection", "0")));
            BrowserTabs.SelectedIndex = (Int32.Parse(SaveHelper.RegRead("ActiveTab", "0")));

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
            MessageBox.Show(info, "Internal Connection Info");
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
                if (heading == "ITEM1" || heading == "ITEM 1" || heading == "EXTERNALID1" || heading == "EXTERNALID1")
                    bOK1 = true;
                if (heading == "ITEM 1" || heading == "ITEM 2" || heading == "EXTERNALID2" || heading == "EXTERNALID 2")
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

                    if (tempArray.Count > 1000)
                    {
                        var s =
                            "Your item query contains too many records (more than 1000). Updating large data sets into SharpCloud may result in stories that are too big to load or have poor performance. Please try refining you query by adding a WHERE clause.";
                        MessageBox.Show(s);
                        tbResults.Text += "\n" + s;
                        tbResults.ScrollToEnd();
                        await Task.Delay(20);
                        return;
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
                    if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage))
                    {
                        MessageBox.Show(errorMessage);
                        tbResults.ScrollToEnd();
                        tbResults.Text += "\nERROR: " + errorMessage;
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

            string strItem1 = "ITEM1";
            bool bItemName1 = true;
            string strItem2 = "ITEM2";
            bool bItemName2 = true;
            bool bDirection = false;
            bool bComment = false;
            bool bTags = false;
            var attributeColumns = new List<RelationshipAttribute>();
            var attributesToCreate = new List<string>();
            var updatedRelationships = new List<Relationship>();
            var attributeValues = new Dictionary<string, Dictionary<Relationship, string>>();

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                tbResults.Text += "\nReading database for relationships";
                tbResults.ScrollToEnd();
                await Task.Delay(20);
                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (!CheckDataIsOKRels(reader))
                    {
                        tbResults.Text += "\nERROR: Invalid SQL";
                        return;
                    }

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var col = reader.GetName(i).ToUpper();
                        if (col == "ITEM 1")
                            strItem1 = "ITEM 1";
                        else if (col == "EXTERNALID1")
                        {
                            bItemName1 = false;
                            strItem1 = "EXTERNALID1";
                        }
                        else if (col == "EXTERNALID 1")
                        {
                            bItemName1 = false;
                            strItem1 = "EXTERNALID 1";
                        }
                        else if (col == "ITEM 2")
                            strItem2 = "ITEM 2";
                        else if (col == "EXTERNALID2")
                        {
                            bItemName2 = false;
                            strItem2 = "EXTERNALID2";
                        }
                        else if (col == "EXTERNALID 2")
                        {
                            bItemName2 = false;
                            strItem2 = "EXTERNALID 2";
                        }
                        else if (col == "COMMENT")
                            bComment = true;
                        else if (col == "DIRECTION")
                            bDirection = true;
                        else if (col == "TAGS")
                            bTags = true;
                        else
                        {
                            if (story.RelationshipAttributes.Any(a => a.Name.ToUpper() == col))
                            {
                                attributeColumns.Add(story.RelationshipAttributes.FirstOrDefault(a => a.Name.ToUpper() == col));
                            } else if (!attributesToCreate.Any(name => name.ToUpper() == col))
                            {
                                var type = reader.GetFieldType(i);

                                if (type == typeof(DateTime))
                                {
                                    var newAttribute = story.RelationshipAttribute_Add(reader.GetName(i), RelationshipAttribute.RelationshipAttributeType.Date);
                                    attributeColumns.Add(newAttribute);
                                }
                                else if (TypeIsNumeric(type))
                                {
                                    var newAttribute = story.RelationshipAttribute_Add(reader.GetName(i), RelationshipAttribute.RelationshipAttributeType.Numeric);
                                    attributeColumns.Add(newAttribute);
                                }
                                else {
                                    attributesToCreate.Add(reader.GetName(i));
                                    attributeValues.Add(reader.GetName(i), new Dictionary<Relationship, string>());
                                }
                            }
                        }
                    }

                    int row = 1;
                    while (reader.Read())
                    {
                        var t1 = reader[strItem1].ToString();
                        var t2 = reader[strItem2].ToString();

                        var i1 = (bItemName1) ? story.Item_FindByName(t1) : story.Item_FindByExternalId(t1);
                        var i2 = (bItemName2) ? story.Item_FindByName(t2) : story.Item_FindByExternalId(t2);

                        if (i1 == null || i2 == null)
                        {
                            tbResults.Text += $"\nERROR: Could not find items '{t1}' or '{t2}' on {row}.";
                        }
                        else
                        {
                            var rel = story.Relationship_FindByItems(i1, i2) ??
                                        story.Relationship_AddNew(i1, i2);
                            if (bComment)
                                rel.Comment = reader["COMMENT"].ToString();
                            if (bDirection)
                            {
                                var txt = reader["DIRECTION"].ToString().Replace(" ", "").ToUpper();
                                if (txt.Contains("BOTH"))
                                    rel.Direction = Relationship.RelationshipDirection.Both;
                                else if (txt.Contains("ATOB") || txt.Contains("1TO2"))
                                    rel.Direction = Relationship.RelationshipDirection.AtoB;
                                else if (txt.Contains("BTOA") || txt.Contains("2TO1"))
                                    rel.Direction = Relationship.RelationshipDirection.Both;
                                else 
                                    rel.Direction = Relationship.RelationshipDirection.None;
                            }
                            if (bTags)
                            {
                                // TODO - delete tags - needs implementing in the SDK        
                                var tags = reader["TAGS"].ToString();
                                foreach (var t in tags.Split(',' ))
                                {
                                    var tag = t.Trim();
                                    if (!string.IsNullOrEmpty(tag))
                                        rel.Tag_AddNew(tag);
                                }
                            }

                            foreach (var att in attributeColumns)
                            {
                                var val = reader[att.Name];

                                if (val == null || val is DBNull || val.ToString() == "(NULL)")
                                {
                                    rel.RemoveAttributeValue(att);
                                }
                                else {
                                    switch (att.Type)
                                    {
                                        case RelationshipAttribute.RelationshipAttributeType.Date:
                                            rel.SetAttributeValue(att, (DateTime)val);
                                            break;
                                        case RelationshipAttribute.RelationshipAttributeType.Numeric:
                                            rel.SetAttributeValue(att, (double)val);
                                            break;
                                        case RelationshipAttribute.RelationshipAttributeType.List:
                                        case RelationshipAttribute.RelationshipAttributeType.Text:
                                            rel.SetAttributeValue(att, val.ToString());
                                            break;
                                    }
                                }
                            }

                            foreach (var newAtt in attributesToCreate)
                            {
                                // Attributes we don't know the type of, keep all the values
                                attributeValues[newAtt].Add(rel, reader[newAtt].ToString());
                            }
                        }
                        row++;
                    }

                    foreach (var item in attributeValues)
                    {
                        var nullCount = 0;
                        var numCount = 0;
                        var dateCount = 0;
                        var labels = new List<string>();
                        var isText = false;
                        double outDouble;
                        DateTime outDateTime;

                        // Find the attribute type
                        foreach(var rel in item.Value)
                        {
                            if (string.IsNullOrEmpty(rel.Value) || rel.Value == "(NULL)")
                            {
                                nullCount++;
                            } else if (double.TryParse(rel.Value, out outDouble))
                            {
                                numCount++;
                            } else if (DateTime.TryParseExact(rel.Value, "yyyy MM dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "yyyy MMM dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "yyyy-MMM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "yyyy/MMM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd MM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd/MMM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                        || DateTime.TryParseExact(rel.Value, "dd/MM/yyyy hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)) {
                                dateCount++;
                            } else {
                                if (!isText)
                                {
                                    if (rel.Value.Length > 100)
                                    {
                                        isText = true;
                                    }
                                    else
                                    {
                                        if (!labels.Contains(rel.Value))
                                        {
                                            labels.Add(rel.Value);
                                        }
                                    }
                                }
                            }
                        }

                        RelationshipAttribute newAttribute;
                        if (dateCount > 0 && dateCount + nullCount == item.Value.Count)
                        {
                            newAttribute = story.RelationshipAttribute_Add(item.Key, RelationshipAttribute.RelationshipAttributeType.Date);
                            foreach (var rel in item.Value)
                            {
                                if (!string.IsNullOrEmpty(rel.Value) && rel.Value != "(NULL)")
                                {
                                    if (DateTime.TryParseExact(rel.Value, "yyyy MM dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "yyyy MMM dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "yyyy-MMM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "yyyy/MMM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd MM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd/MMM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime)
                          || DateTime.TryParseExact(rel.Value, "dd/MM/yyyy hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime))
                                    {
                                        rel.Key.SetAttributeValue(newAttribute, outDateTime);
                                    }
                                }
                            }
                        }
                        else if (numCount > 0 && numCount + nullCount == item.Value.Count)
                        {
                            newAttribute = story.RelationshipAttribute_Add(item.Key, RelationshipAttribute.RelationshipAttributeType.Numeric);
                            foreach (var rel in item.Value) {
                                if (!string.IsNullOrEmpty(rel.Value) && rel.Value != "(NULL)")
                                {                                    
                                    if (double.TryParse(rel.Value, out outDouble))
                                    {
                                        rel.Key.SetAttributeValue(newAttribute, outDouble);
                                    }
                                }
                            }
                        }
                        else {
                            if (!isText && (labels.Count + nullCount < item.Value.Count))
                            {
                                newAttribute = story.RelationshipAttribute_Add(item.Key, RelationshipAttribute.RelationshipAttributeType.List);
                            }
                            else
                            {
                                newAttribute = story.RelationshipAttribute_Add(item.Key, RelationshipAttribute.RelationshipAttributeType.Text);
                            }
                            foreach (var rel in item.Value)
                            {
                                if (!string.IsNullOrEmpty(rel.Value) && rel.Value != "(NULL)")
                                {
                                    rel.Key.SetAttributeValue(newAttribute, rel.Value);
                                }
                            }
                        }
                    }
                }
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

                if (ConnectionString.Text.Contains("\""))
                    MessageBox.Show(
                        "Your connection string and/or query string contains '\"', which will automatically be replaced with '");
                try
                {
                    CopyResourceFile(folder, "Newtonsoft.Json.dll");
                    CopyResourceFile(folder, "SC.Framework.dll");
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
                content = content.Replace("LOGFILE", $"{folder}\\Logfile.txt");

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
            var remote = string.Format("{0}/{1}", "https://sharpcloudonpremupdate.blob.core.windows.net:443/apidemos/sharpcloudSQLUpdate/SQLBatch2", filename);
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
            }
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            var qd = connectionList.SelectedItem as QueryData;
            var qdNew = new QueryData(qd);
            _connections.Add(qdNew);
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
                txtDatabaseType.Text = qd.ConnectionType.ToString();
                ConnectionString.Text = qd.ConnectionsString;
                SQLString.Text = qd.QueryString;
                StoryId.Text = qd.StoryId;
                SQLStringRels.Text = qd.QueryStringRels;
                FileName.Text = qd.FileName;
                SharePointURL.Text = qd.SharePointURL;
                txtExampleRels.Text = "Example: " + qd.GetExampleRelQuery;

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
            if (s.Contains("#/story"))
            {
                var mid = s.Substring(s.IndexOf("#/story") + 8);
                if (mid.Length > 36)
                {
                    mid = mid.Substring(0, 36);
                    StoryId.Text = mid;
                }
            }
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
            return new SharpCloudApi(Username.Text, Password.Password, Url.Text);
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
    }
}
