using SCQueryConnect.Common.Interfaces;
using System.Data;

namespace SCQueryConnect.Common.Helpers
{
    public class DataChecker : IDataChecker
    {
        public bool CheckDataIsOK(IDataReader reader)
        {
            bool bOK = false;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var heading = reader.GetName(i).ToUpper();
                if (heading == "NAME")
                    bOK = true;
                else if (heading == "EXTERNAL ID")
                    bOK = true;
                else if (heading == "EXTERNALID")
                    bOK = true;
            }

            ProcessDataValidity(bOK);
            return bOK;
        }

        protected virtual void ProcessDataValidity(bool isOk)
        {
        }
    }
}
