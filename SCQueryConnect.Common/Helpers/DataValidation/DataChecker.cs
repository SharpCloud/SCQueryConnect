using SCQueryConnect.Common.Interfaces.DataValidation;
using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers.DataValidation
{
    public abstract class DataChecker : IDataChecker
    {
        public IDataValidityProcessor ValidityProcessor { get; set; }

        public async Task<bool> CheckData(IDataReader reader)
        {
            return await CheckData(reader, null);
        }

        public async Task<bool> CheckData(IDataReader reader, object state)
        {
            var isOk = await CheckDataIsValid(reader);
            ValidityProcessor?.ProcessDataValidity(isOk, state);
            return isOk;
        }

        protected abstract Task<bool> CheckDataIsValid(IDataReader reader);
    }
}
