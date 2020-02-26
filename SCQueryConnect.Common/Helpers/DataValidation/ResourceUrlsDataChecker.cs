using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers.DataValidation
{
    public class ResourceUrlsDataChecker : DataChecker, IResourceUrlsDataChecker
    {
        public const string ExternalIdHeader = "ExternalID";
        public const string ResourceNameHeader = "ResourceName";
        public const string DescriptionHeader = "Description";
        public const string UrlHeader = "URL";

        public static readonly HashSet<string> RequiredHeadings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ExternalIdHeader,
            ResourceNameHeader,
            DescriptionHeader,
            UrlHeader
        };

        private readonly ILog _logger;

        public ResourceUrlsDataChecker(ILog logger)
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
                await _logger.LogWarning($"Resource URL data invalid - headings must contain all of {valid}");
            }

            return isOk;
        }
    }
}
