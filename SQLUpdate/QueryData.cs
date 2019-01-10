using SCQueryConnect.Common;
using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

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
        [IgnoreDataMember]
        public DataView QueryResults { get; set; }
        [IgnoreDataMember]
        public DataView QueryResultsRels { get; set; }

        public string FormattedConnectionString => ConnectionsString.Replace("{0}", FileName).Replace("{1}", SharePointURL);

        public string GetBatchDBType
        {
            get
            {
                switch (ConnectionType)
                {
                    case DatabaseType.SQL:
                        return DatabaseTypeStrings.Sql;
                    case DatabaseType.ODBC:
                        return DatabaseTypeStrings.Odbc;
                    case DatabaseType.SharpCloud:
                        return DatabaseTypeStrings.SharpCloud;
                }
                return DatabaseTypeStrings.Oledb; // most types are ADO
            }
        }

        public DbConnection GetDb()
        {
            switch (ConnectionType)
            {
                case DatabaseType.SQL:
                    return new SqlConnection(FormattedConnectionString);
                case DatabaseType.ODBC:
                    return new OdbcConnection(FormattedConnectionString);
                case DatabaseType.SharpCloud:
                    var excelConnectionString = Regex.Replace(
                        ConnectionsString,
                        "Source Story=.+?;",
                        string.Empty,
                        RegexOptions.IgnoreCase);

                    return new OleDbConnection(excelConnectionString);
                default:
                    return new OleDbConnection(FormattedConnectionString);
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
                case DatabaseType.SharpCloud:
                    Name = "SharpCloud Example";
                    ConnectionsString =
                        "Source Story=00000000-0000-0000-0000-000000000000;" +
                        "Provider=Microsoft.ACE.OLEDB.12.0;" +
                        @"Data Source=C:\MyFolder\MyFile.xlsx;" +
                        "Extended Properties='Excel 12.0 Xml;" +
                        "HDR = YES'";

                    QueryString = "SELECT * from [Sheet1$]";
                    QueryStringRels = ""; // "/*Uncomment to use*/\n/*SELECT * from [Sheet2$]*/";
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
