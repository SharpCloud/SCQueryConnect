using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SCSQLBatch
{
    public class ConsoleLogger : Logger
    {
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
            var LogFile = ConfigurationManager.AppSettings["LogFile"];
            if (!string.IsNullOrEmpty(LogFile) && LogFile != "LOGFILE")
            {
                try
                {
                    var helper = new PathHelper();
                    var path = helper.GetAbsolutePath(LogFile);
                    File.AppendAllText(path, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to {LogFile}");
                    Console.WriteLine($"{ex.Message}");
                }
            }

            Debug.Write(message);
            Console.Write(message);
        }
        #pragma warning restore 1998
    }
}
