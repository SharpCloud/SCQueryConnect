using SCQueryConnect.Common.Interfaces;
using System.Data;

namespace SCQueryConnect.Common.Helpers
{
    public class ResourceUrlDataChecker : DataChecker, IResourceUrlDataChecker
    {
        protected override bool CheckDataIsValid(IDataReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}
