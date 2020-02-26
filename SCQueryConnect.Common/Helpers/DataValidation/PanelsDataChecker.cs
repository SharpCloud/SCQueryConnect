using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers.DataValidation
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

        private readonly ILog _logger;

        public PanelsDataChecker(ILog logger)
        {
            _logger = logger;
        }

        protected override async Task<bool> CheckDataIsValid(IDataReader reader)
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

            if (!isOk)
            {
                var valid = string.Join(", ", RequiredHeadings.Select(h => $"'{h}'"));
                await _logger.LogWarning($"Panels data invalid - headings must contain all of {valid}");
            }

            return isOk;
        }
    }
}
