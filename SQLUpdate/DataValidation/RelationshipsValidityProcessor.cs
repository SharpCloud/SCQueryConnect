using SCQueryConnect.Common.Interfaces.DataValidation;
using SCQueryConnect.Models;

namespace SCQueryConnect.DataValidation
{
    public class RelationshipsValidityProcessor : IDataValidityProcessor
    {
        public void ProcessDataValidity(bool isOk, object state)
        {
            if (state is QueryData queryData)
            {
                queryData.IsRelationshipsQueryOk = isOk;
            }
        }
    }
}
