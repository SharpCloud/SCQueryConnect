using SCQueryConnect.Common;

namespace SCQueryConnect.Logging
{
    public abstract class LoggingDestination : Logger
    {
    }

    public abstract class LoggingDestination<T> : LoggingDestination
    {
        protected readonly T Destination;

        protected LoggingDestination(T destination)
        {
            Destination = destination;
        }
    }
}
