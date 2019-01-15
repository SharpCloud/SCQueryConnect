using System.Data;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection GetDb(string connectionString, DatabaseType dbType);
    }
}
