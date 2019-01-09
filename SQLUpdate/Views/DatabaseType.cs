namespace SCQueryConnect.Views
{
    public class DatabaseType
    {
        public QueryData.DbType DBType { get; set; }
        public string Name { get; set; }

        public DatabaseType(QueryData.DbType dbType, string name)
        {
            DBType = dbType;
            Name = name;
        }
    }
}
