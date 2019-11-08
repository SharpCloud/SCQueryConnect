using SCQueryConnect.Common.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SCQueryConnect.Common
{
    public class ConsoleLogger : Logger
    {
        private readonly string _logFile;

        public ConsoleLogger(string logFile)
        {
            _logFile = logFile;
        }

        public override Task Clear()
        {
            throw new NotSupportedException();
        }

        #pragma warning disable 1998
        public override async Task Log(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var message = FormatMessage(text);
            if (!string.IsNullOrEmpty(_logFile) && _logFile != "LOGFILE")
            {
                try
                {
                    var helper = new PathHelper();
                    var path = helper.GetAbsolutePath(_logFile);
                    File.AppendAllText(path, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to {_logFile}");
                    Console.WriteLine($"{ex.Message}");
                }
            }

            Debug.Write(message);
            Console.Write(message);
        }
        #pragma warning restore 1998
    }
}
