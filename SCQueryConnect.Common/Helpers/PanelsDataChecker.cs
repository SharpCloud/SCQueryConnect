using SCQueryConnect.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SCQueryConnect.Common.Helpers
{
    public class PanelsDataChecker : DataChecker, IPanelsDataChecker
    {
        public const string ExternalIdHeader = "ExternalID";
        public const string TitleHeader = "Title";
        public const string PanelTypeHeader = "PanelType";
        public const string DataHeader = "Data";

        public static HashSet<string> RequiredHeadings = new HashSet<string>
        {
            ExternalIdHeader,
            TitleHeader,
            PanelTypeHeader,
            DataHeader
        };

        protected override bool CheckDataIsValid(IDataReader reader)
        {
            var isOk = false;

            var required = RequiredHeadings.ToDictionary(
                k => k,
                k => false,
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; !isOk && i < reader.FieldCount; i++)
            {
                var heading = reader.GetName(i);

                if (required.ContainsKey(heading))
                {
                    required[heading] = true;
                }

                isOk = required.Values.All(v => v == true);
            }

            return isOk;
        }
    }
}
