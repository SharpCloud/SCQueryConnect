using SCQueryConnect.Common.Interfaces;
using System.Data;
using System.Linq;

namespace SCQueryConnect.Common.Helpers
{
    public class RelationshipsDataChecker : DataChecker, IRelationshipsDataChecker
    {
        private readonly string[] _validItem1Headings =
        {
            "ITEM1",
            "ITEM 1",
            "EXTERNALID1",
            "EXTERNALID 1",
            "EXTERNAL ID 1",
            "INTERNAL ID 1"
        };

        private readonly string[] _validItem2Headings =
        {
            "ITEM2",
            "ITEM 2",
            "EXTERNALID2",
            "EXTERNALID 2",
            "EXTERNAL ID 2",
            "INTERNAL ID 2"
        };

        protected override bool CheckDataIsValid(IDataReader reader)
        {
            bool bOK1 = false;
            bool bOK2 = false;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var heading = reader.GetName(i).ToUpper();

                if (_validItem1Headings.Contains(heading))
                    bOK1 = true;

                if (_validItem2Headings.Contains(heading))
                    bOK2 = true;
            }

            var isOk = bOK1 && bOK2;
            return isOk;
        }
    }
}
