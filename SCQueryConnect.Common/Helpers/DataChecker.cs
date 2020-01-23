using SCQueryConnect.Common.Interfaces;
using System.Data;

namespace SCQueryConnect.Common.Helpers
{
    public abstract class DataChecker : IDataChecker
    {
        public IDataCheckerValidityProcessor ValidityProcessor { get; set; }

        public bool CheckData(IDataReader reader)
        {
            var isOk = CheckDataIsValid(reader);
            ValidityProcessor?.ProcessDataValidity(isOk);
            return isOk;
        }

        protected abstract bool CheckDataIsValid(IDataReader reader);
    }
}
