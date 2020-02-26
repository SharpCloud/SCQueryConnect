using SCQueryConnect.Common.Interfaces.DataValidation;
using SCQueryConnect.Models;

namespace SCQueryConnect.DataValidation
{
    public class ItemsValidityProcessor : IDataValidityProcessor
    {
        public void ProcessDataValidity(bool isOk, object state)
        {
            if (state is QueryData queryData)
            {
                queryData.IsItemsQueryOk = isOk;
            }
        }
    }
}
