using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
        public DbType ConnectionType { get; set; }
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

        public string FormattedConnectionString => string.Format(ConnectionsString, FileName, SharePointURL);

        public string GetBatchDBType
        {
            get
            {
                switch (ConnectionType)
                {
                    case DbType.SQL:
                        return "SQL";
                    case DbType.ODBC:
                        return "ODBC";
                }
                return "OLEDB"; // most types are ADO
            }
        }

        public DbConnection GetDb()
        {
            switch (ConnectionType)
            {
                case QueryData.DbType.SQL:
                    return new SqlConnection(FormattedConnectionString);
                case QueryData.DbType.ODBC:
                    return new OdbcConnection(FormattedConnectionString);
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

        public QueryData(DbType type)
        {
            ConnectionType = type;
            switch (type)
            {
                case QueryData.DbType.SQL:
                    Name = "SQL Server Example";
                    FileName = "";
                    ConnectionsString = "Server=.; Integrated Security=true; Database=demo";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";
                    // "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE*/";
                    break;
                case QueryData.DbType.ODBC:
                    Name = "ODBC Example";
                    FileName = "";
                    ConnectionsString = "DSN=DatasourceName";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";
                    //    "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
                    break;
                case QueryData.DbType.ADO:
                    Name = "ADO/OLEDB Example";
                    FileName = "";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\myFolder\\myAccessFile.accdb;";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";//"SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
                    break;
                case QueryData.DbType.Excel:
                    Name = "Excel Example";
                    FileName = "C:/MyFolder/MyFile.xlsx";
                    ConnectionsString =
                        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 Xml; HDR = YES'";
                    QueryString = "SELECT * from [Sheet1$]";
                    QueryStringRels = ""; // "/*Uncomment to use*/\n/*SELECT * from [Sheet2$]*/";
                    break;
                case QueryData.DbType.Access:
                    Name = "Access Example";
                    FileName = "C:/MyFolder/MyFile.accdb";
                    ConnectionsString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}";
                    QueryString = "SELECT * FROM TABLE";
                    QueryStringRels = "";
                    //    "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE*/";
                    break;
                case QueryData.DbType.SharepointList:
                    Name = "SharePoint List Example";
                    SharePointURL = "https://mysite.sharepoint.com;LIST={LISTGUID}";
                    ConnectionsString = "Provider=Microsoft.ACE.OLEDB.12.0;WSS;IMEX=2;RetrieveIds=Yes;DATABASE={1}";
                    QueryString = "SELECT * FROM LISTITEM";
                    QueryStringRels = ""; 
                    //    "/*Uncomment to use*/\n/*SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE*/";
                    break;
            }
        }

        public string GetExampleRelQuery
        {
            get
            {
                switch (ConnectionType)
                {
                    case QueryData.DbType.Excel:
                        return "SELECT * from [Sheet2$]";
                }
                return "SELECT ITEM1, ITEM2, COMMENT, DIRECTION, TAGS FROM RELTABLE";
            }
        }


        public enum DbType
        {
            SQL,
            ODBC,
            ADO,
            Excel,
            Access,
            SharepointList
        }
    }
}
