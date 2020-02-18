using Newtonsoft.Json;
using SCQueryConnect.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SCQueryConnect.Models
{
    public class QueryData : INotifyPropertyChanged
    {
        public const string RootId = "RootId";

        private static readonly char[] NewLineSeparators = {'\r', '\n'};

        private readonly Regex _errorRegex = new Regex(Logger.ErrorPrefix);
        private readonly Regex _warningRegex = new Regex(Logger.WarningPrefix);

        private bool _dragAbove;
        private bool _dragBelow;
        private bool _dragInto;
        private bool _isExpanded;
        private bool _isSelected;
        private string _id;
        private string _name;
        private string _description;
        private ObservableCollection<QueryData> _connections;
        private string _fileName;
        private string _sharePointUrl;
        private string _storyId;
        private bool _buildRelationships;
        private bool _unpublishItems;
        private string _issueSummary;
        private string _logData;

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

        public DatabaseType ConnectionType { get; set; }
        
        public string ConnectionsString { get; set; }

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

        public string QueryString { get; set; }

        public string QueryStringRels { get; set; }
        
        public string QueryStringPanels { get; set; }
        
        public string QueryStringResourceUrls { get; set; }

        public string FileName
        {
            get => _fileName;

            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public DateTime? LastRunDateTime { get; set; }

        public string LogData
        {
            get => _logData;

            set
            {
                _logData = value;

                if (_logData == null)
                {
                    return;
                }

                var entries = _logData.Split(NewLineSeparators);
                var errorCount = entries.Count(_errorRegex.IsMatch);
                var warningCount = entries.Count(_warningRegex.IsMatch);
                IssueSummary = $"{errorCount} error(s), {warningCount} warning(s)";
            }
        }

        public string SourceStoryId { get; set; }
        
        public string SourceStoryUserName { get; set; }
        
        public string SourceStoryPassword { get; set; }
        
        public string SourceStoryPasswordDpapi { get; set; }
        
        public string SourceStoryPasswordEntropy { get; set; }
        
        public string SourceStoryServer { get; set; }

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

        [JsonIgnore]
        public bool IsFolder => Connections != null;

        [JsonIgnore]
        public DataView QueryResults { get; set; }

        [JsonIgnore]
        public DataView QueryResultsRels { get; set; }
        
        [JsonIgnore]
        public DataView QueryResultsPanels { get; set; }
        
        [JsonIgnore]
        public DataView QueryResultsResourceUrls { get; set; }

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

        [JsonIgnore]
        public string FormattedConnectionString => ConnectionsString
            ?.Replace("{0}", FileName)
            .Replace("{1}", SharePointURL)
            .Replace("{source-story-id}", SourceStoryId)
            .Replace("{source-story-user-name}", SourceStoryUserName)
            .Replace("{source-story-password}", SourceStoryPassword)
            .Replace("{source-story-server}", SourceStoryServer);

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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
        
        [JsonIgnore]
        public bool DragAbove
        {
            get => _dragAbove;

            set
            {
                if (_dragAbove != value)
                {
                    _dragAbove = value;
                    OnPropertyChanged();
                }
            }
        }
        
        [JsonIgnore]
        public bool DragBelow
        {
            get => _dragBelow;

            set
            {
                if (_dragBelow != value)
                {
                    _dragBelow = value;
                    OnPropertyChanged();
                }
            }
        }
        
        [JsonIgnore]
        public bool DragInto
        {
            get => _dragInto;

            set
            {
                if (_dragInto != value)
                {
                    _dragInto = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public string IssueSummary
        {
            get => _issueSummary;

            set
            {
                if (_issueSummary != value)
                {
                    _issueSummary = value;
                    OnPropertyChanged();
                }
            }
        }

        public QueryData()
        {
        }

        public QueryData(QueryData qd)
        {
            Description = string.IsNullOrWhiteSpace(qd.Description)
                ? string.Empty
                : "Copy of " + qd.Description;
            
            Name = qd.Name + " Copy";
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

        public override string ToString()
        {
            return Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
