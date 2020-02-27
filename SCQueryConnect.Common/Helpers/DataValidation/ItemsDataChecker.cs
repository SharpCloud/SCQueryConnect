using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers.DataValidation
{
    public class ItemsDataChecker : DataChecker
    {
        private static readonly HashSet<string> ValidHeadings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Name",
            "External ID",
            "ExternalID"
        };

        private readonly ILog _logger;

        public override QueryEntityType TargetEntity { get; } = QueryEntityType.Items;

        public ItemsDataChecker(ILog logger)
        {
            _logger = logger;
        }

        protected override async Task<bool> CheckDataIsValid(IDataReader reader)
        {
            var isOk = false;

            for (int i = 0; i < reader.FieldCount && !isOk; i++)
            {
                var heading = reader.GetName(i);
                isOk = ValidHeadings.Contains(heading);
            }

            if (!isOk)
            {
                var valid = string.Join(", ", ValidHeadings.Select(h => $"'{h}'"));
                await _logger.LogWarning($"Item data invalid - headings must contain one of {valid}");
            }

            return isOk;
        }
    }
}
