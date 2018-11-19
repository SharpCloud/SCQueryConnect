using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using System.Globalization;

namespace SCSQLBatchDelete
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", "SCSQLBatch.exe.config");

            var userid = ConfigurationManager.AppSettings["userid"];
            var password = ConfigurationManager.AppSettings["password"];
            var password64 = ConfigurationManager.AppSettings["password64"];
            var url = ConfigurationManager.AppSettings["url"];
            var storyid = ConfigurationManager.AppSettings["storyid"];
            var connectionString = ConfigurationManager.AppSettings["connectionString"];
            var queryString = ConfigurationManager.AppSettings["queryString"];
            var queryStringRels = ConfigurationManager.AppSettings["queryStringRels"];
            bool unpubItems;
//            if (bool.TryParse(ConfigurationManager.AppSettings["unpublishItems"], out unpubItems))
//                unpublishItems = unpubItems;
            var proxy = ConfigurationManager.AppSettings["proxy"];
            bool proxyAnonymous = true;
            bool.TryParse(ConfigurationManager.AppSettings["proxyAnonymous"], out proxyAnonymous);
            var proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
            var proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
            var proxyPassword64 = ConfigurationManager.AppSettings["proxyPassword64"];

            // basic checks
            if (string.IsNullOrEmpty(userid) || userid == "USERID")
            {
                Log("Error: No username provided.");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                // set the password from the encoded password
                password = Encoding.Default.GetString(Convert.FromBase64String(password64));
                if (string.IsNullOrEmpty(password64))
                {
                    Log("Error: No password provided.");
                    return;
                }
            }
            if (string.IsNullOrEmpty(url))
            {
                Log("Error: No URL provided.");
                return;
            }
            if (string.IsNullOrEmpty(storyid) || userid == "00000000-0000-0000-0000-000000000000")
            {
                Log("Error: No storyID provided.");
                return;
            }
            if (string.IsNullOrEmpty(connectionString) || connectionString == "CONNECTIONSTRING")
            {
                Log("Error: No connection string provided.");
                return;
            }
            if (string.IsNullOrEmpty(queryString) || userid == "QUERYSTRING")
            {
                Log("Error: No database query provided.");
                return;
            }
            if (!string.IsNullOrEmpty(proxy) && !proxyAnonymous)
            {
                if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
                {
                    Log("Error: No proxy username or password provided.");
                }
                if (string.IsNullOrEmpty(proxyPassword))
                {
                    proxyPassword = Encoding.Default.GetString(Convert.FromBase64String(proxyPassword64));
                }
            }
            // do the work

            try
            {
                Log($"Starting Deletion process.");
                var start = DateTime.UtcNow;

                // create our connection
                var sc = new SharpCloudApi(userid, password, url, proxy, proxyAnonymous, proxyUsername, proxyPassword);
                Log($"Load story.");
                var story = sc.LoadStory(storyid);
                Log($"Story loaded.");

                var toDelete = new List<Item>();

                foreach (var i in story.Items)
                {
                    if (!i.IsPublished)
                        toDelete.Add(i);
                }

                foreach (var i in toDelete)
                {
                    Log($"Deleting $'{i.Name}'");
                    story.Item_DeleteById(i.Id);
                }

                story.Save();
                Log($"Deleted $'{toDelete.Count}'");

            }
            catch (Exception e)
            {
                Log("Error: " + e.Message);
            }

        }
        
        private static void Log(string text)
        {
            var now = DateTime.UtcNow;
            text = now.ToShortDateString() + " " + now.ToLongTimeString() + " " + text + "\r\n";
            var LogFile = ConfigurationManager.AppSettings["LogFile"];
            if (!string.IsNullOrEmpty(LogFile) && LogFile != "LOGFILE")
            {
                try
                {
                    File.AppendAllText(LogFile, text);
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
