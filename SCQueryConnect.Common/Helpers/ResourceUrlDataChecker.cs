﻿using SCQueryConnect.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SCQueryConnect.Common.Helpers
{
    public class ResourceUrlDataChecker : DataChecker, IResourceUrlDataChecker
    {
        public const string ExternalIdHeader = "ExternalId";
        public const string ResourceNameHeader = "ResourceName";
        public const string DescriptionHeader = "Description";
        public const string UrlHeader = "URL";

        public static HashSet<string> RequiredHeadings = new HashSet<string>
        {
            ExternalIdHeader,
            ResourceNameHeader,
            DescriptionHeader,
            UrlHeader
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
