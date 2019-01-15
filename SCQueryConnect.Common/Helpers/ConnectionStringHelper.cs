using System.Text.RegularExpressions;

namespace SCQueryConnect.Common.Helpers
{
    public class ConnectionStringHelper : IConnectionStringHelper
    {
        public string GetVariable(string connectionString, string variableName)
        {
            var kvp = Regex.Match(
                connectionString,
                $"{variableName}=.*?;",
                RegexOptions.IgnoreCase).Value.Trim(';');

            var split = kvp.Split('=');

            var value = split.Length > 1
                ? split[1]
                : string.Empty;

            return value;
        }
    }
}
