using System.Text.RegularExpressions;

namespace SCQueryConnect.Common.Helpers
{
    public class ConnectionStringHelper
    {
        public string GetVariable(string connectionString, string variableName)
        {
            var kvp = Regex.Match(
                connectionString,
                $"{variableName}=.+?;",
                RegexOptions.IgnoreCase).Value.Trim(';');

            var value = kvp.Split('=')[1];
            return value;
        }
    }
}
