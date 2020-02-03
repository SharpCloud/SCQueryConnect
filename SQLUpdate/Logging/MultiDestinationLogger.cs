using SCQueryConnect.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCQueryConnect.Logging
{
    public class MultiDestinationLogger : Logger
    {
        private readonly Stack<LoggingDestination> _loggingDestinations =
            new Stack<LoggingDestination>();

        public void PopLoggingDestination()
        {
            _loggingDestinations.Pop();
        }

        public void PushLoggingDestination(LoggingDestination destination)
        {
            _loggingDestinations.Push(destination);
        }

        public override async Task Clear()
        {
            foreach (var destination in _loggingDestinations)
            {
                await destination.Clear();
            }
        }

        public override async Task Log(string text)
        {
            foreach (var destination in _loggingDestinations)
            {
                await destination.Log(text);
            }
        }
    }
}
