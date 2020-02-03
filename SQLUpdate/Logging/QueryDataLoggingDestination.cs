using SCQueryConnect.Models;
using System.Threading.Tasks;

namespace SCQueryConnect.Logging
{
    public class QueryDataLoggingDestination : LoggingDestination<QueryData>
    {
        public QueryDataLoggingDestination(QueryData queryData) : base(queryData)
        {
        }

        public override async Task Log(string text)
        {
            var toAppend = FormatMessage(text);
            Destination.LogData += toAppend;
            await Task.CompletedTask;
        }

        public override async Task Clear()
        {
            Destination.LogData = string.Empty;
            await Task.CompletedTask;
        }
    }
}
