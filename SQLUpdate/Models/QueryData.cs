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
        private const string FilenamePlaceHolder = "{filename}";

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
        private string _fileName = string.Empty;
        private string _sharePointUrl = string.Empty;
        private bool _buildRelationships;
        private bool _unpublishItems;
        private string _issueSummary;
        private string _logData;
        private DataTable _queryResults;
        private DataTable _queryResultsRels;
        private DataTable _queryResultsPanels;
        private DataTable _queryResultsResourceUrls;

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

        public string StoryId { get; set; }

        public string QueryString { get; set; } = "SELECT * FROM TABLE";

        public string QueryStringRels { get; set; } = string.Empty;

        public string QueryStringPanels { get; set; } = string.Empty;

        public string QueryStringResourceUrls { get; set; } = string.Empty;

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
        public DataTable QueryResults
        {
            get => _queryResults;

            set
            {
                if (_queryResults != value)
                {
                    _queryResults = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public DataTable QueryResultsRels
        {
            get => _queryResultsRels;

            set
            {
                if (_queryResultsRels != value)
                {
                    _queryResultsRels = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public DataTable QueryResultsPanels
        {
            get => _queryResultsPanels;
            set
            {
                if (_queryResultsPanels != value)
                {
                    _queryResultsPanels = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public DataTable QueryResultsResourceUrls
        {
            get => _queryResultsResourceUrls;

            set
            {
                if (_queryResultsResourceUrls != value)
                {
                    _queryResultsResourceUrls = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public string FormattedConnectionString => ConnectionsString
            ?.Replace("{0}", FileName)
            .Replace(FilenamePlaceHolder, FileName)
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

                var success = DatabaseStrings.TypeStringMapping.TryGetValue(
                    ConnectionType,
                    out var dbString);

                if (!success)
                {
                    dbString = DatabaseStrings.Oledb;
                }

                return dbString;
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
                        return "SELECT * from Sheet2";
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
            // Do not copy this for a copy

            Description = string.IsNullOrWhiteSpace(qd.Description)
                ? string.Empty
                : "Copy of " + qd.Description;
            
            Name = qd.Name + " Copy";
            ConnectionType = qd.ConnectionType;
            ConnectionsString = qd.ConnectionsString;
            QueryString = qd.QueryString;
            QueryStringRels = qd.QueryStringRels;
            QueryStringResourceUrls = qd.QueryStringResourceUrls;
            QueryStringPanels = qd.QueryStringPanels;
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
                    ConnectionsString = "Server=.; Integrated Security=true; Database=demo";
                    break;
                
                case DatabaseType.ODBC:
                    Name = "ODBC Example";
                    ConnectionsString = "DSN=DatasourceName";
                    break;
                
                case DatabaseType.ADO:
                    Name = "ADO/OLEDB Example";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\myFolder\\myAccessFile.accdb;";
                    break;
                
                case DatabaseType.Excel:
                    Name = "Excel Example";
                    FileName = "C:/MyFolder/MyFile.xlsx";
                    ConnectionsString = $"Excel File={FilenamePlaceHolder}";
                    QueryString = "SELECT * from Sheet1";
                    break;
                
                case DatabaseType.Access:
                    Name = "Access Example";
                    FileName = "C:/MyFolder/MyFile.accdb";
                    ConnectionsString = $"Data Source={FilenamePlaceHolder}";
                    break;
                
                case DatabaseType.SharepointList:
                    Name = "SharePoint List Example";
                    SharePointURL = "User=USERNAME;Password=PASSWORD;Auth Scheme=NTLM;URL=http://sharepointserver/mysite;";
                    ConnectionsString = "{1}";
                    QueryString = "SELECT * FROM LIST";
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
                        $"Excel File={FilenamePlaceHolder}";
                    QueryString = "SELECT * FROM Items";
                    QueryStringRels = "SELECT * FROM Relationships";
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
