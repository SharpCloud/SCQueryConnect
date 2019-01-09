using SCQueryConnect.Common.Interfaces;
using SCSQLBatch.Helpers;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SCSQLBatch
{
    public class ConsoleLogger : ILog
    {
        public Task Clear()
        {
            throw new NotSupportedException();
        }

        public async Task Log(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var now = DateTime.UtcNow;
            text = now.ToShortDateString() + " " + now.ToLongTimeString() + " " + text + "\r\n";
            var LogFile = ConfigurationManager.AppSettings["LogFile"];
            if (!string.IsNullOrEmpty(LogFile) && LogFile != "LOGFILE")
            {
                try
                {
                    var helper = new LogHelper();
                    var path = helper.GetAbsolutePath(LogFile);
                    File.AppendAllText(path, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to {LogFile}");
                    Console.WriteLine($"{ex.Message}");
                }
            }

            Debug.Write(text);
            Console.Write(text);
        }
    }
}
