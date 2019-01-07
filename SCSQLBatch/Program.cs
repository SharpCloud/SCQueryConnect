using SC.API.ComInterop;
using SC.API.ComInterop.ArrayProcessing;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common;
using SCSQLBatch.Helpers;
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
using System.Text;

namespace SCSQLBatch
{
    class Program
    {
        private readonly string[] _validItem1Headings =
        {
            "ITEM 1",
            "EXTERNALID1",
            "EXTERNALID 1",
            "EXTERNAL ID 1",
            "INTERNAL ID 1"
        };

        private readonly string[] _validItem2Headings =
{
            "ITEM 2",
            "EXTERNALID2",
            "EXTERNALID 2",
            "EXTERNAL ID 2",
            "INTERNAL ID 2"
        };

        static bool unpublishItems = false;

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
            var qcHelper = new QueryConnectHelper();

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
                Log($"Starting process.");
                var start = DateTime.UtcNow;

                // create our connection
                var sc = new SharpCloudApi(userid, password, url, proxy, proxyAnonymous, proxyUsername, proxyPassword);
                var story = sc.LoadStory(storyid);
                var isValid = qcHelper.Validate(story, out var message);
                Log(message);

                if (isValid)
                {
                    using (DbConnection connection = GetDb(connectionString))
                    {
                        connection.Open();
                        UpdateItems(connection, story, queryString);
                        UpdateRelationships(connection, story, queryStringRels);
                        Log("Saving");
                        story.Save();
                        Log($"Process completed in {(DateTime.UtcNow - start).Seconds} seconds.");
                    }
                }
            }
            catch (Exception e)
            {
                Log("Error: " + e.Message);
            }
        }

        private static void UpdateItems(DbConnection connection, Story story, string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString)) // nothing to do
                return;

            Log("Updating items");
            
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
                            Log(errorMessage);
                        }
                        else
                        {
                            Log(errorMessage);
                        }
                    }
                    else {
                        if (story.UpdateStoryWithArray(arrayValues, false, out errorMessage))
                        {
                            Log(errorMessage);
                        }
                        else
                        {
                            Log(errorMessage);
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

        private static void UpdateRelationships(DbConnection connection, Story story, string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString)) // nothing to do
                return;

            Log("Updating relationships");
            
            var attributeColumns = new List<RelationshipAttribute>();
            var attributesToCreate = new List<string>();
            var updatedRelationships = new List<Relationship>();
            var attributeValues = new Dictionary<string, Dictionary<Relationship, string>>();

            int rowCount;

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = queryString;
                command.CommandType = CommandType.Text;

                int columnCount;
                var dataList = new List<string[]>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    // Write array column headers

                    columnCount = reader.FieldCount;
                    for (int i = 0; i < columnCount; i++)
                    {
                        dataList.Add(new string[columnCount]);
                        dataList[0][i] = reader.GetName(i).ToUpper();
                    }

                    // Write array data

                    while (reader.Read())
                    {
                        var dataRow = new string[columnCount];
                        dataList.Add(dataRow);

                        for (int i = 0; i < columnCount; i++)
                        {
                            dataRow[i] = reader[i].ToString();
                        }
                    }
                }

                // Create data array to pass to SharpCloud

                var filteredData = dataList.Where(strArray =>
                    strArray.Any(s => !string.IsNullOrWhiteSpace(s)))
                    .ToList();

                var data = new string[filteredData.Count, columnCount];

                for (int row = 0; row < filteredData.Count; row++)
                {
                    for (int column = 0; column < columnCount; column++)
                    {
                        data[row, column] = filteredData[row][column];
                    }
                }
                
                rowCount = filteredData.Count - 1; // -1 for headings

                var updater = new RelationshipsUpdater();
                updater.UpdateRelationships(data, story);
            }
            Log($"{rowCount} rows processed.");
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
         
        private static void Log(string text)
        {
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
