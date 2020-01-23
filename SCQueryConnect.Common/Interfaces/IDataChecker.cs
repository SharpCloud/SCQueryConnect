using System.Data;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IDataChecker
    {
        bool CheckData(IDataReader reader);
    }
}
