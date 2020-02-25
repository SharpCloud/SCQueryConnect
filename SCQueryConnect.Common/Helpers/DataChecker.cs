using SCQueryConnect.Common.Interfaces;
using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers
{
    public abstract class DataChecker : IDataChecker
    {
        public IDataCheckerValidityProcessor ValidityProcessor { get; set; }

        public async Task<bool> CheckData(IDataReader reader)
        {
            var isOk = await CheckDataIsValid(reader);
            ValidityProcessor?.ProcessDataValidity(isOk);
            return isOk;
        }

        protected abstract Task<bool> CheckDataIsValid(IDataReader reader);
    }
}
