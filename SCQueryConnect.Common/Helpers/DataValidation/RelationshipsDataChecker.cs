using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers.DataValidation
{
    public class RelationshipsDataChecker : DataChecker
    {
        private static readonly HashSet<string> ValidItem1Headings =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Item1",
                "Item 1",
                "ExternalID1",
                "ExternalID 1",
                "External ID 1",
                "Internal ID 1"
            };

        private static readonly HashSet<string> ValidItem2Headings =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Item2",
                "Item 2",
                "ExternalID2",
                "ExternalID 2",
                "External ID 2",
                "Internal ID 2"
            };

        private readonly ILog _logger;

        public override QueryEntityType TargetEntity { get; } = QueryEntityType.Relationships;

        public RelationshipsDataChecker(ILog logger)
        {
            _logger = logger;
        }

        protected override async Task<bool> CheckDataIsValid(IDataReader reader)
        {
            var item1Ok = false;
            var item2Ok = false;
            var commentOk = false;
            var directionOk = false;

            var allOk = false;

            for (int i = 0; i < reader.FieldCount && !allOk; i++)
            {
                var heading = reader.GetName(i).ToUpper();

                item1Ok = item1Ok || ValidItem1Headings.Contains(heading);
                item2Ok = item2Ok || ValidItem2Headings.Contains(heading);
                commentOk = commentOk || string.Equals(heading, "Comment", StringComparison.OrdinalIgnoreCase);
                directionOk = directionOk || string.Equals(heading, "Direction", StringComparison.OrdinalIgnoreCase);

                allOk = item1Ok && item2Ok && commentOk && directionOk;
            }

            if (!allOk)
            {
                var valid1 = string.Join(", ", ValidItem1Headings.Select(h => $"'{h}'"));
                var valid2 = string.Join(", ", ValidItem2Headings.Select(h => $"'{h}'"));

                await _logger.LogWarning($"Relationships data invalid - headings must contain one of [{valid1}]; one of [{valid2}]; 'Comment'; 'Direction'");
            }

            return allOk;
        }
    }
}
