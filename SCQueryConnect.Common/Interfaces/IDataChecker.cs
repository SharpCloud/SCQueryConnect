using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IDataChecker
    {
        IDataCheckerValidityProcessor ValidityProcessor { get; set; }

        Task<bool> CheckData(IDataReader reader);
    }
}
