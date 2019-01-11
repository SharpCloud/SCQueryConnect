using System.Data;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IDataChecker
    {
        bool CheckDataIsOK(IDataReader reader);
    }
}
