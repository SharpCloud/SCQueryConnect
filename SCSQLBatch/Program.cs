using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Text;

namespace SCSQLBatch
{
    class Program
    {
        static bool unpublishItems = false;
        static ILog logger = new ConsoleLogger();

        static void Main(string[] args)
        {
            var userid = ConfigurationManager.AppSettings["userid"];
            var password = ConfigurationManager.AppSettings["password"];
            var password64 = ConfigurationManager.AppSettings["password64"];
            var url = ConfigurationManager.AppSettings["url"];
            var storyid = ConfigurationManager.AppSettings["storyid"];
            var connectionString = ConfigurationManager.AppSettings["connectionString"];
            var queryString = ConfigurationManager.AppSettings["queryString"];
            var queryStringRels = ConfigurationManager.AppSettings["queryStringRels"];
            bool unpubItems;
            if (bool.TryParse(ConfigurationManager.AppSettings["unpublishItems"], out unpubItems))
                unpublishItems = unpubItems;
            var proxy = ConfigurationManager.AppSettings["proxy"];
            bool proxyAnonymous = true;
            bool.TryParse(ConfigurationManager.AppSettings["proxyAnonymous"], out proxyAnonymous);
            var proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
            var proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
            var proxyPassword64 = ConfigurationManager.AppSettings["proxyPassword64"];

            var qcHelper = new QueryConnectHelper(
                logger,
                new RelationshipsDataChecker());

            // basic checks
            if (string.IsNullOrEmpty(userid) || userid == "USERID")
            {
                logger.Log("Error: No username provided.").Wait();
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                // set the password from the encoded password
                password = Encoding.Default.GetString(Convert.FromBase64String(password64));
                if (string.IsNullOrEmpty(password64))
                {
                    logger.Log("Error: No password provided.").Wait();
                    return;
                }
            }
            if (string.IsNullOrEmpty(url))
            {
                logger.Log("Error: No URL provided.").Wait();
                return;
            }
            if (string.IsNullOrEmpty(storyid) || userid == "00000000-0000-0000-0000-000000000000")
            {
                logger.Log("Error: No storyID provided.").Wait();
                return;
            }
            if (string.IsNullOrEmpty(connectionString) || connectionString == "CONNECTIONSTRING")
            {
                logger.Log("Error: No connection string provided.").Wait();
                return;
            }
            if (string.IsNullOrEmpty(queryString) || userid == "QUERYSTRING")
            {
                logger.Log("Error: No database query provided.").Wait();
                return;
            }
            if (!string.IsNullOrEmpty(proxy) && !proxyAnonymous)
            {
                if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
                {
                    logger.Log("Error: No proxy username or password provided.").Wait();
                }
                if (string.IsNullOrEmpty(proxyPassword))
                {
                    proxyPassword = Encoding.Default.GetString(Convert.FromBase64String(proxyPassword64));
                }
            }
            // do the work

            try
            {
                logger.Log($"Starting process.").Wait();
                var start = DateTime.UtcNow;

                // create our connection
                var sc = new SharpCloudApi(userid, password, url, proxy, proxyAnonymous, proxyUsername, proxyPassword);
                var story = sc.LoadStory(storyid);
                var isValid = qcHelper.Validate(story, out var message);
                logger.Log(message).Wait();

                if (isValid)
                {
                    using (DbConnection connection = GetDb(connectionString))
                    {
                        connection.Open();
                        UpdateItems(connection, story, queryString);
                        qcHelper.UpdateRelationships(connection, story, queryStringRels).Wait();
                        logger.Log("Saving").Wait();
                        story.Save();
                        logger.Log($"Process completed in {(DateTime.UtcNow - start).Seconds} seconds.").Wait();
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log("Error: " + e.Message).Wait();
            }
        }

        private static async void UpdateItems(DbConnection connection, Story story, string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString)) // nothing to do
                return;

            await logger.Log("Updating items");
            
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = queryString;
                command.CommandType = CommandType.Text;

                using (DbDataReader reader = command.ExecuteReader())
                {
                    var tempArray = new List<List<string>>();
                    while (reader.Read())
                    {
                        var objs = new object[reader.FieldCount];
                        reader.GetValues(objs);
                        var data = new List<string>();
                        foreach (var o in objs)
                        {
                            if (o is DateTime)
                            {
                                var date = (DateTime)o;
                                data.Add(date.ToString("yyyy MM dd"));
                            }
                            else
                            {
                                DateTime date;
                                double dbl;
                                var s = o.ToString();
                                if (double.TryParse(s, out dbl))
                                {
                                    data.Add($"{dbl:0.##}");
                                }
                                else if (DateTime.TryParse(s, out date))
                                {
                                    data.Add(date.ToString("yyyy MM dd"));
                                }
                                else if (s.ToLower().Trim() == "null")
                                {
                                    data.Add("");
                                }
                                else
                                {
                                    data.Add(s);
                                }
                            }
                        }
                        tempArray.Add(data);
                    }

                    // create our string arrar
                    var arrayValues = new string[tempArray.Count + 1, reader.FieldCount];
                    // add the headers
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        arrayValues[0, i] = reader.GetName(i);
                    }
                    // add the data values
                    int row = 1;
                    foreach (var list in tempArray)
                    {
                        int col = 0;
                        foreach (string s in list)
                        {
                            arrayValues[row, col++] = s;
                        }
                        row++;
                    }

                    // pass the array to SharpCloud
                    string errorMessage;

                    if (unpublishItems)
                    {
                        List<Guid> updatedItems;
                        if (story.UpdateStoryWithArray(arrayValues, false, out errorMessage, out updatedItems))
                        {
                            foreach (var item in story.Items)
                            {
                                item.AsElement.IsInRoadmap = updatedItems.Contains(item.AsElement.ID);
                            }
                            await logger.Log(errorMessage);
                        }
                        else
                        {
                            await logger.Log(errorMessage);
                        }
                    }
                    else {
                        if (story.UpdateStoryWithArray(arrayValues, false, out errorMessage))
                        {
                            await logger.Log(errorMessage);
                        }
                        else
                        {
                            await logger.Log(errorMessage);
                        }
                    }
                }
            }
        }

        private static bool TypeIsNumeric(Type type)
        {
            return type == typeof(double) || type == typeof(int) || type == typeof(float) || type == typeof(decimal) ||
                type == typeof(short) || type == typeof(long) || type == typeof(byte) || type == typeof(SByte) ||
                type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64);
        }

        private static DbConnection GetDb(string connectionString)
        {
            var dbType = ConfigurationManager.AppSettings["dbType"];
            switch (dbType)
            {
                default:
                case "SQL":
                    return new SqlConnection(connectionString);
                case "ODBC":
                    return new OdbcConnection(connectionString);
                case "OLEDB":
                    return new OleDbConnection(connectionString);
            }
        }
    }
}
