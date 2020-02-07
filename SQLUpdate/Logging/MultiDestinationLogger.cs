using SCQueryConnect.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Logging
{
    public class MultiDestinationLogger : Logger
    {
        private readonly Stack<LoggingDestination> _temporaryDestinations =
            new Stack<LoggingDestination>();

        private IList<LoggingDestination> _persistentLoggingDestinations;

        private IEnumerable<LoggingDestination> AllLoggingDestinations =>
            _persistentLoggingDestinations.Concat(_temporaryDestinations);

        public void PopDestination()
        {
            _temporaryDestinations.Pop();
        }

        public void SetPersistentDestination(params LoggingDestination[] destinations)
        {
            _persistentLoggingDestinations = destinations;
        }

        public void ClearDestinations()
        {
            _temporaryDestinations.Clear();
        }

        public void PushDestination(LoggingDestination destination)
        {
            _temporaryDestinations.Push(destination);
        }

        public override async Task Clear()
        {
            foreach (var destination in AllLoggingDestinations)
            {
                await destination.Clear();
            }
        }

        public override async Task Log(string text)
        {
            foreach (var destination in AllLoggingDestinations)
            {
                await destination.Log(text);
            }
        }
    }
}
