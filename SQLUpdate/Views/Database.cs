using SCQueryConnect.Common;

namespace SCQueryConnect.Views
{
    public class Database
    {
        public DatabaseType DBType { get; set; }
        public string Name { get; set; }

        public Database(DatabaseType dbType, string name)
        {
            DBType = dbType;
            Name = name;
        }
    }
}
