using SC.API.ComInterop;
using SC.API.ComInterop.ArrayProcessing;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers
{
    public class QueryConnectHelper
    {
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IDataChecker _dataChecker;
        private readonly ILog _logger;
        private readonly IRelationshipsDataChecker _relationshipsDataChecker;

        public QueryConnectHelper(
            IConnectionStringHelper connectionStringHelper,
            IDataChecker dataChecker,
            ILog log,
            IRelationshipsDataChecker relationshipsDataChecker)
        {
            _connectionStringHelper = connectionStringHelper;
            _dataChecker = dataChecker;
            _logger = log;
            _relationshipsDataChecker = relationshipsDataChecker;
        }

        public string GetStoryUrl(string input)
        {
            if (input.Contains("#/story"))
            {
                var mid = input.Substring(input.IndexOf("#/story") + 8);
                if (mid.Length >= 36)
                {
                    mid = mid.Substring(0, 36);
                    return mid;
                }
            }

            return input;
        }

        public bool Validate(Story story, out string message)
        {
            if (story.Categories.Length == 0)
            {
                message = "Aborting update: story has no categories";
                return false;
            }

            message = $"Reading story '{story.Name}'";
            return true;
        }

        public async Task UpdateRelationships(DbConnection connection, Story story, string sqlString)
        {
            if (string.IsNullOrWhiteSpace(sqlString))
            {
                await _logger.Log("No Relationship Query detected");
                return;
            }

            int rowCount;

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                await _logger.Log("Reading database for relationships");

                int columnCount;
                var dataList = new List<string[]>();
                
                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (!_relationshipsDataChecker.CheckDataIsOKRels(reader))
                    {
                        await _logger.Log("\nERROR: Invalid SQL");
                        return;
                    }

                    // Write array column headers

                    columnCount = reader.FieldCount;
                    for (int i = 0; i < columnCount; i++)
                    {
                        dataList.Add(new string[columnCount]);
                        dataList[0][i] = reader.GetName(i);
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

            await _logger.Log($"{rowCount} rows processed.");
        }

        public void InitialiseDatabase(
            SharpCloudApi sharpCloudApi,
            string connectionString,
            DatabaseType dbType)
        {
            if (dbType == DatabaseType.SharpCloud)
            {
                var filename = _connectionStringHelper.GetVariable(connectionString, "Data Source");
                var sourceId = _connectionStringHelper.GetVariable(connectionString, DatabaseStrings.SharpCloudSourceStory);

                var story = sharpCloudApi.LoadStory(sourceId);
                var items = story.GetItemsData();
                var relationships = story.GetRelationshipsData();

                var writer = new ExcelWriter();
                writer.WriteToExcel(filename, items, relationships);
            }
        }

        public async Task UpdateSharpCloud(
            SharpCloudConfiguration config,
            UpdateSettings settings)
        {
            try
            {
                var start = DateTime.Now;
                await _logger.Log($"Starting update process...");
                await _logger.Log("Connecting to Sharpcloud " + config.Url);

                var sc = new SharpCloudApi(
                    config.Username,
                    config.Password,
                    config.Url,
                    config.ProxyUrl,
                    config.UseDefaultProxyCredentials,
                    config.ProxyUserName,
                    config.ProxyPassword);

                var story = sc.LoadStory(settings.TargetStoryId);
                var isValid = Validate(story, out var message);
                await _logger.Log(message);

                if (isValid)
                {
                    InitialiseDatabase(sc, settings.ConnectionString, settings.DBType);

                    using (DbConnection connection = GetDb(settings.ConnectionString, settings.DBType))
                    {
                        connection.Open();

                        await UpdateItems(
                            connection,
                            story,
                            settings.QueryString,
                            settings.MaxRowCount,
                            settings.UnpublishItems);

                        await UpdateRelationships(connection, story, settings.QueryStringRels);
                        await _logger.Log("Saving Changes");
                        story.Save();
                        await _logger.Log("Save Complete!");
                        await _logger.Log($"Update process completed in {(DateTime.Now - start).TotalSeconds:f2} seconds");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.Log("Error: " + ex.Message);
            }
        }

        public DbConnection GetDb(string connectionString, DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.SQL:
                {
                    return new SqlConnection(connectionString);
                }

                case DatabaseType.ODBC:
                {
                    return new OdbcConnection(connectionString);
                }

                case DatabaseType.SharpCloud:
                {
                    const string delimiter = ";";

                    var kvps = connectionString
                        .Split(delimiter[0])
                        .Where(kvp => !kvp.ToLower().StartsWith("source story"))
                        .ToArray();

                    var excelConnectionString = string.Join(delimiter, kvps);
                    return new OleDbConnection(excelConnectionString);
                }
                default:
                {
                    return new OleDbConnection(connectionString);
                }
            }
        }

        private async Task UpdateItems(
            DbConnection connection,
            Story story,
            string sqlString,
            int maxRowCount,
            bool unpublishItems)
        {
            if (string.IsNullOrWhiteSpace(sqlString))
            {
                await _logger.Log("No Item Query detected");
                return;
            }

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                await _logger.Log("Reading database");
                using (DbDataReader reader = command.ExecuteReader())
                {
                    _dataChecker.CheckDataIsOK(reader);

                    var tempArray = new List<List<string>>();
                    while (reader.Read())
                    {
                        var objs = new object[reader.FieldCount];
                        reader.GetValues(objs);
                        var data = new List<string>();
                        foreach (var o in objs)
                        {
                            if (o is DateTime?)
                            {
                                // definately date time
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

                    if (tempArray.Count > maxRowCount)
                    {
                        var s = $"Your item query contains too many records (more than {maxRowCount}). Updating large data sets into SharpCloud may result in stories that are too big to load or have poor performance. Please try refining you query by adding a WHERE clause.";
                        await _logger.Log(s);
                        return;
                    }

                    // create our string arrar
                    var arrayValues = new string[tempArray.Count + 1, reader.FieldCount];
                    // add the headers
                    var regex = new Regex(Regex.Escape("#"));
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var header = reader.GetName(i);
                        if (header.ToLower().StartsWith("tags#"))
                        {
                            header = regex.Replace(header, ".", 1);
                        }
                        arrayValues[0, i] = header;
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

                    await _logger.Log($"Processing {row.ToString()} rows");

                    // pass the array to SharpCloud
                    string errorMessage;
                    if (unpublishItems)
                    {
                        List<Guid> updatedItems;
                        if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage, out updatedItems))
                        {
                            await _logger.Log(errorMessage);
                        }
                        else
                        {
                            foreach (var item in story.Items)
                            {
                                if (!updatedItems.Contains(item.AsElement.ID))
                                {
                                    try
                                    {
                                        item.IsPublished = false;
                                    }
                                    catch (FieldAccessException ex)
                                    {
                                        await _logger.Log($"ERROR: {ex.Message}");
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                await _logger.Log(errorMessage);
                            }
                        }
                    }
                    else
                    {
                        if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage))
                        {
                            await _logger.Log(errorMessage);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                await _logger.Log(errorMessage);
                            }
                        }
                    }
                }
            }
        }
    }
}
