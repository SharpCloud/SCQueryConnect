using System.Data;

namespace SCQueryConnect.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection GetDb(QueryData queryData);
    }
}
