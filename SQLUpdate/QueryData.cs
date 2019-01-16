using SCQueryConnect.Common;
using System;
using System.Data;
using System.Runtime.Serialization;

namespace SCQueryConnect
{
    [DataContract]
    public class QueryData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public DatabaseType ConnectionType { get; set; }
        [DataMember]
        public string ConnectionsString { get; set; }
        [DataMember]
        public string QueryString { get; set; }
        [DataMember]
        public string StoryId { get; set; }
        [DataMember]
        public string QueryStringRels { get; set; }
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public string SharePointURL { get; set; }
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
        public string SourceStoryServer { get; set; }
        [IgnoreDataMember]
        public DataView QueryResults { get; set; }
        [IgnoreDataMember]
        public DataView QueryResultsRels { get; set; }

        public string FormattedConnectionString => ConnectionsString
            .Replace("{0}", FileName)
            .Replace("{1}", SharePointURL)
            .Replace("{source-story-id}", SourceStoryId)
            .Replace("{source-story-user-name}", SourceStoryUserName)
            .Replace("{source-story-password}", SourceStoryPassword)
            .Replace("{source-story-server}", SourceStoryServer);

        public string GetBatchDBType
        {
            get
            {
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
                    QueryStringRels = "";
                    // "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE*/";
                    break;
                case DatabaseType.ODBC:
                    Name = "ODBC Example";
                    FileName = "";
                    SharePointURL = "";
                    ConnectionsString = "DSN=DatasourceName";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";
                    //    "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
                    break;
                case DatabaseType.ADO:
                    Name = "ADO/OLEDB Example";
                    FileName = "";
                    SharePointURL = "";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\myFolder\\myAccessFile.accdb;";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";//"SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
                    break;
                case DatabaseType.Excel:
                    Name = "Excel Example";
                    FileName = "C:/MyFolder/MyFile.xlsx";
                    SharePointURL = "";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 Xml; HDR = YES'";
                    QueryString = "SELECT * from [Sheet1$]";
                    QueryStringRels = ""; // "/*Uncomment to use*/\n/*SELECT * from [Sheet2$]*/";
                    break;
                case DatabaseType.Access:
                    Name = "Access Example";
                    FileName = "C:/MyFolder/MyFile.accdb";
                    SharePointURL = "";
                    ConnectionsString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";
                    //    "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE*/";
                    break;
                case DatabaseType.SharepointList:
                    Name = "SharePoint List Example";
                    SharePointURL = "https://mysite.sharepoint.com;LIST={LISTGUID}";
                    ConnectionsString = "Provider=Microsoft.ACE.OLEDB.12.0;WSS;IMEX=2;RetrieveIds=Yes;DATABASE={1}";
                    QueryString = "SELECT * FROM LISTITEM";
                    QueryStringRels = ""; 
                    //    "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE*/";
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
                    QueryStringRels = "SELECT * from [Relationships$]";
                    break;
            }
        }

        public string GetExampleRelQuery
        {
            get
            {
                switch (ConnectionType)
                {
                    case DatabaseType.Excel:
                        return "SELECT * from [Sheet2$]";
                }
                return "SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
            }
        }
    }
}
