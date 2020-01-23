using SCQueryConnect.Common.Interfaces;
using System.Data;

namespace SCQueryConnect.Common.Helpers
{
    public class ItemDataChecker : DataChecker, IItemDataChecker
    {
        protected override bool CheckDataIsValid(IDataReader reader)
        {
            var isOk = false;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var heading = reader.GetName(i).ToUpper();
                
                if (heading == "NAME")
                    isOk = true;
                else if (heading == "EXTERNAL ID")
                    isOk = true;
                else if (heading == "EXTERNALID")
                    isOk = true;
            }

            return isOk;
        }
    }
}
