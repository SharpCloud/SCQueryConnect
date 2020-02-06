using SCQueryConnect.Common.Interfaces;
using System;
using System.Threading.Tasks;

namespace SCQueryConnect.Common
{
    public abstract class Logger : ILog
    {
        public const string ErrorPrefix = "ERROR:";
        public const string WarningPrefix = "WARNING:";

        public abstract Task Clear();
        public abstract Task Log(string text);

        public async Task LogError(string text)
        {
            await Log($"{ErrorPrefix} {text}");
        }

        public async Task LogWarning(string text)
        {
            await Log($"{WarningPrefix} {text}");
        }

        protected string FormatMessage(string message)
        {
            var now = DateTime.UtcNow;
            var timestamp = $"{now.ToShortDateString()} {now.ToLongTimeString()}";

            var lineEnd = message.EndsWith(Environment.NewLine)
                ? string.Empty
                : Environment.NewLine;
            
            return $"[{timestamp}] {message}{lineEnd}";
        }
    }
}
