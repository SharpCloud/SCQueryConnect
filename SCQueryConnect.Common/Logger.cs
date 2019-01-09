using SCQueryConnect.Common.Interfaces;
using System;
using System.Threading.Tasks;

namespace SCQueryConnect.Common
{
    public abstract class Logger : ILog
    {
        public abstract Task Clear();
        public abstract Task Log(string text);

        protected string FormatMessage(string message)
        {
            var now = DateTime.UtcNow;
            var timestamp = $"{now.ToShortDateString()} {now.ToLongTimeString()}";
            return $"{timestamp} {message}{Environment.NewLine}";
        }
    }
}
