using System.Data;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IDataChecker
    {
        IDataCheckerValidityProcessor ValidityProcessor { get; set; }

        bool CheckData(IDataReader reader);
    }
}
