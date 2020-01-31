using SCQueryConnect.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace SCQueryConnect.Models
{
    [DataContract]
    public class QueryData
    {
        public const string RootId = "RootId";

        private bool _isExpanded;
        private bool _isSelected;
        private string _id;
        private string _name;
        private string _description;
        private QueryData _parentFolder;
        private ObservableCollection<QueryData> _connections;
        private string _sharePointUrl;
        private string _storyId;
        private bool _buildRelationships;
        private bool _unpublishItems;

        [IgnoreDataMember]
        public bool IsFolder => Connections != null;

        [DataMember]
        public bool IsExpanded
        {
            get => _isExpanded;

            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        [IgnoreDataMember]
        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public string Id
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    _id = Guid.NewGuid().ToString();
                }

                return _id;
            }
            
            set => _id = value;
        }

        [DataMember]
        public string Name
        {
            get => _name;

            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public string Description
        {
            get => _description;

            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public DatabaseType ConnectionType { get; set; }
        [DataMember]
        public string ConnectionsString { get; set; }
        [DataMember]
        public string QueryString { get; set; }

        [DataMember]
        public string StoryId
        {
            get => _storyId;

            set
            {
                var newValue = value;
                if (newValue != null && newValue.Contains("#/story"))
                {
                    var mid = newValue.Substring(newValue.IndexOf("#/story", StringComparison.Ordinal) + 8);
                    if (mid.Length >= 36)
                    {
                        newValue = mid.Substring(0, 36);
                    }
                }

                _storyId = newValue;
            }
        }

        [DataMember]
        public string QueryStringRels { get; set; }
        [DataMember]
        public string QueryStringPanels { get; set; }
        [DataMember]
        public string QueryStringResourceUrls { get; set; }
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string SharePointURL
        {
            get => _sharePointUrl;
            set
            {
                var newValue = value;

                // note - the list ID can be found by reading data from the following url
                // SharePointURL/_api/web/lists
                // TODO give the user a way of seacrching for the list ID..
                // currently this can easily be done from Excel.

                if (newValue != null &&
                    newValue.ToUpper().Contains("LIST="))
                {
                    // user has provided a good link with a specified list.
                    // make sure the list is a guid 
                    newValue = newValue.Replace("%7B", "{").Replace("%2D", "-").Replace("%7D", "}");
                }

                _sharePointUrl = newValue;
            }
        }

        [DataMember]
        public DateTime? LastRunDateTime { get; set; }
        [DataMember]
        public string LogData { get; set; }
        [DataMember]
        public string SourceStoryId { get; set; }
        [DataMember]
        public string SourceStoryUserName { get; set; }
        [DataMember]
        public string SourceStoryPassword { get; set; }
        [DataMember]
        public string SourceStoryPasswordDpapi { get; set; }
        [DataMember]
        public string SourceStoryPasswordEntropy { get; set; }
        [DataMember]
        public string SourceStoryServer { get; set; }

        [DataMember]
        public bool BuildRelationships
        {
            get => _buildRelationships;

            set
            {
                if (_buildRelationships != value)
                {
                    _buildRelationships = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public bool UnpublishItems
        {
            get => _unpublishItems;

            set
            {
                if (_unpublishItems != value)
                {
                    _unpublishItems = value;
                    OnPropertyChanged();
                }
            }
        }

        [IgnoreDataMember]
        public DataView QueryResults { get; set; }
        [IgnoreDataMember]
        public DataView QueryResultsRels { get; set; }
        [IgnoreDataMember]
        public DataView QueryResultsPanels { get; set; }
        [IgnoreDataMember]
        public DataView QueryResultsResourceUrls { get; set; }
        [DataMember]
        public ObservableCollection<QueryData> Connections
        {
            get => _connections;

            set
            {
                if (_connections != value)
                {
                    _connections = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormattedConnectionString => ConnectionsString
            ?.Replace("{0}", FileName)
            .Replace("{1}", SharePointURL)
            .Replace("{source-story-id}", SourceStoryId)
            .Replace("{source-story-user-name}", SourceStoryUserName)
            .Replace("{source-story-password}", SourceStoryPassword)
            .Replace("{source-story-server}", SourceStoryServer);

        public string GetBatchDBType
        {
            get
            {
                if (IsFolder)
                {
                    return null;
                }

                switch (ConnectionType)
                {
                    case DatabaseType.SQL:
                        return DatabaseStrings.Sql;
                    case DatabaseType.ODBC:
                        return DatabaseStrings.Odbc;
                    case DatabaseType.SharpCloudExcel:
                        return DatabaseStrings.SharpCloudExcel;
                }
                return DatabaseStrings.Oledb; // most types are ADO
            }
        }

        public string LastRunDate
        {
            get
            {
                if (LastRunDateTime == null)
                    return "Never";

                if (LastRunDateTime.Value.ToShortDateString() == DateTime.Now.ToShortDateString())
                    return $"TODAY at {LastRunDateTime.Value:HH:mm:ss}";

                return $"{LastRunDateTime.Value:dd MMM yyyy HH:mm:ss}";
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public QueryData()
        {
        }

        public QueryData(QueryData qd)
        {
            Name = qd.Name + " Copy";
            Description = "Copy of " + qd.Description;
            ConnectionType = qd.ConnectionType;
            ConnectionsString = qd.ConnectionsString;
            //StoryId = qd.StoryId; // do not copy this for a copy
            QueryString = qd.QueryString;
            QueryStringRels = qd.QueryStringRels;
            FileName = qd.FileName;
            SharePointURL = qd.SharePointURL;

            if (qd.IsFolder)
            {
                Connections = new ObservableCollection<QueryData>();

                foreach (var c in qd.Connections)
                {
                    Connections.Add(new QueryData(c));
                }
            }
        }

        public QueryData(DatabaseType type)
        {
            ConnectionType = type;
            switch (type)
            {
                case DatabaseType.SQL:
                    Name = "SQL Server Example";
                    FileName = "";
                    SharePointURL = "";
                    ConnectionsString = "Server=.; Integrated Security=true; Database=demo";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringPanels = "";
                    QueryStringRels = "";
                    QueryStringResourceUrls = "";
                    break;
                case DatabaseType.ODBC:
                    Name = "ODBC Example";
                    FileName = "";
                    SharePointURL = "";
                    ConnectionsString = "DSN=DatasourceName";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringPanels = "";
                    QueryStringRels = "";
                    QueryStringResourceUrls = "";
                    break;
                case DatabaseType.ADO:
                    Name = "ADO/OLEDB Example";
                    FileName = "";
                    SharePointURL = "";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\myFolder\\myAccessFile.accdb;";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringPanels = "";
                    QueryStringRels = "";
                    QueryStringResourceUrls = "";
                    break;
                case DatabaseType.Excel:
                    Name = "Excel Example";
                    FileName = "C:/MyFolder/MyFile.xlsx";
                    SharePointURL = "";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 Xml; HDR = YES'";
                    QueryString = "SELECT * from [Sheet1$]";
                    QueryStringPanels = "";
                    QueryStringRels = "";
                    QueryStringResourceUrls = "";
                    break;
                case DatabaseType.Access:
                    Name = "Access Example";
                    FileName = "C:/MyFolder/MyFile.accdb";
                    SharePointURL = "";
                    ConnectionsString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringPanels = "";
                    QueryStringRels = "";
                    QueryStringResourceUrls = "";
                    break;
                case DatabaseType.SharepointList:
                    Name = "SharePoint List Example";
                    SharePointURL = "https://mysite.sharepoint.com;LIST={LISTGUID}";
                    ConnectionsString = "Provider=Microsoft.ACE.OLEDB.12.0;WSS;IMEX=2;RetrieveIds=Yes;DATABASE={1}";
                    QueryString = "SELECT * FROM LISTITEM";
                    QueryStringPanels = "";
                    QueryStringRels = "";
                    QueryStringResourceUrls = "";
                    break;
                case DatabaseType.SharpCloudExcel:
                    Name = "SharpCloud (Excel) Example";
                    FileName = @"C:\MyFolder\MyFile.xlsx";
                    SourceStoryId = "00000000-0000-0000-0000-000000000000";
                    ConnectionsString =
                        "SourceId={source-story-id};" +
                        "SourceUserName={source-story-user-name};" +
                        "SourcePassword={source-story-password};" +
                        "SourceServer={source-story-server};" +
                        "Provider=Microsoft.ACE.OLEDB.12.0;" +
                        "Data Source={0};" +
                        "Extended Properties='Excel 12.0 Xml;" +
                        "HDR = YES'";

                    QueryString = "SELECT * from [Items$]";
                    QueryStringPanels = "";
                    QueryStringRels = "SELECT * from [Relationships$]";
                    QueryStringResourceUrls = "";
                    break;
            }
        }

        public string ExampleRelQuery
        {
            get
            {
                if (IsFolder)
                {
                    return null;
                }

                switch (ConnectionType)
                {
                    case DatabaseType.Excel:
                        return "SELECT * from [Sheet2$]";
                }
                return "SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
