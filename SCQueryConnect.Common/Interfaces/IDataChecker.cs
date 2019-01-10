using System.Data.Common;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IDataChecker
    {
        bool CheckDataIsOK(DbDataReader reader);
    }
}
