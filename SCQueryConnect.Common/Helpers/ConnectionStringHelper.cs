﻿using SCQueryConnect.Common.Interfaces;
using System.Text.RegularExpressions;

namespace SCQueryConnect.Common.Helpers
{
    public class ConnectionStringHelper : IConnectionStringHelper
    {
        public string GetVariable(string connectionString, string variableName)
        {
            var kvp = Regex.Match(
                connectionString,
                $"{variableName}=(.*?)(;|$)",
                RegexOptions.IgnoreCase).Value.Trim(';');

            var value = Regex.Match(
                kvp,
                $"{variableName}=(.*)",
                RegexOptions.IgnoreCase).Groups[1].Value;

            return value;
        }

        public string SetDataSource(string connectionString, string newLocation)
        {
            var updated = Regex.Replace(
                connectionString,
                $"{DatabaseStrings.DataSourceKey}=(.*?)(;|$)",
                m => $"{DatabaseStrings.DataSourceKey}={newLocation}{m.Groups[2]}");

            return updated;
        }
    }
}
