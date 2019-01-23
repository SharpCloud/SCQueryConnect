using SC.API.ComInterop;
using SC.API.ComInterop.ArrayProcessing;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Helpers
{
    public class QueryConnectHelper
    {
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IDataChecker _dataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IExcelWriter _excelWriter;
        private readonly ILog _logger;
        private readonly IRelationshipsDataChecker _relationshipsDataChecker;
        private readonly ISharpCloudApiFactory _sharpCloudApiFactory;
        private readonly Regex _tagHeaderRegex = new Regex(Regex.Escape("#"));

        public QueryConnectHelper(
            IConnectionStringHelper connectionStringHelper,
            IDataChecker dataChecker,
            IDbConnectionFactory dbConnectionFactory,
            IExcelWriter excelWriter,
            ILog log,
            IRelationshipsDataChecker relationshipsDataChecker,
            ISharpCloudApiFactory sharpCloudApiFactory)
        {
            _connectionStringHelper = connectionStringHelper;
            _dataChecker = dataChecker;
            _dbConnectionFactory = dbConnectionFactory;
            _excelWriter = excelWriter;
            _logger = log;
            _relationshipsDataChecker = relationshipsDataChecker;
            _sharpCloudApiFactory = sharpCloudApiFactory;
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

        private string GetHeaderName(string headerText)
        {
            if (headerText.ToLower().StartsWith("tags#"))
            {
                var updated = _tagHeaderRegex.Replace(headerText, ".", 1);
                return updated;
            }

            return headerText;
        }

        public async Task UpdateRelationships(IDbConnection connection, Story story, string sqlString)
        {
            if (string.IsNullOrWhiteSpace(sqlString))
            {
                await _logger.Log("No Relationship Query detected");
                return;
            }

            int rowCount;

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                await _logger.Log("Reading database for relationships");

                int columnCount;
                var dataList = new List<string[]>();
                
                using (IDataReader reader = command.ExecuteReader())
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

                        var headerText = reader.GetName(i);
                        var header = GetHeaderName(headerText);
                        dataList[0][i] = header;
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

        /// <summary>
        /// Load data from SharpCloud and save it to an Excel file. Only needed when
        /// SharpCloud is a data source, so the upload process can work on local data.
        /// </summary>
        public async Task InitialiseDatabase(
            SharpCloudConfiguration config,
            string connectionString,
            DatabaseType dbType)
        {
            if (dbType == DatabaseType.SharpCloudExcel)
            {
                var filename = _connectionStringHelper.GetVariable(connectionString, DatabaseStrings.DataSourceKey);
                var sourceId = _connectionStringHelper.GetVariable(connectionString, "SourceId");

                var csUsername = _connectionStringHelper.GetVariable(connectionString, "SourceUserName");
                var csPassword = _connectionStringHelper.GetVariable(connectionString, "SourcePassword");
                var csServer = _connectionStringHelper.GetVariable(connectionString, "SourceServer");

                var username = string.IsNullOrWhiteSpace(csUsername)
                    ? config.Username
                    : csUsername;

                var password = string.IsNullOrWhiteSpace(csPassword)
                    ? config.Password
                    : Encoding.Default.GetString(Convert.FromBase64String(csPassword));

                var server = string.IsNullOrWhiteSpace(csServer)
                    ? config.Url
                    : csServer;

                await _logger.Log("Initialising data source...");

                var sc = _sharpCloudApiFactory.CreateSharpCloudApi(
                    username,
                    password,
                    server,
                    config.ProxyUrl,
                    config.UseDefaultProxyCredentials,
                    config.ProxyUserName,
                    config.ProxyPassword);

                if (sc == null)
                {
                    throw new InvalidCredentialsException();
                }

                var story = sc.LoadStory(sourceId);
                var items = story.GetItemsData();
                var relationships = story.GetRelationshipsData();

                await _logger.Log($"Writing story to {filename}");
                _excelWriter.WriteToExcel(filename, items, relationships);
                await _logger.Log("Data source ready");
            }
        }

        public async Task UpdateSharpCloud(
            SharpCloudConfiguration config,
            UpdateSettings settings)
        {
            string newFilename = null;
            var generateTempFile = false;

            try
            {
                var sc = _sharpCloudApiFactory.CreateSharpCloudApi(
                    config.Username,
                    config.Password,
                    config.Url,
                    config.ProxyUrl,
                    config.UseDefaultProxyCredentials,
                    config.ProxyUserName,
                    config.ProxyPassword);

                if (sc == null)
                {
                    await _logger.Log(InvalidCredentialsException.LoginFailed);
                    return;
                }

                var start = DateTime.Now;

                await _logger.Log("Starting update process...");
                await _logger.Log("Connecting to Sharpcloud " + config.Url);

                var story = sc.LoadStory(settings.TargetStoryId);
                var isValid = Validate(story, out var message);
                await _logger.Log(message);

                if (isValid)
                {
                    var filename = _connectionStringHelper.GetVariable(
                        settings.ConnectionString,
                        DatabaseStrings.DataSourceKey);

                    var withoutExt = Path.GetFileNameWithoutExtension(filename);
                    generateTempFile = string.IsNullOrWhiteSpace(withoutExt);
                    var pathHelper = new PathHelper();
                    newFilename = pathHelper.GetAbsolutePath($"{Guid.NewGuid()}.xlsx");

                    var connectionString = generateTempFile
                        ? _connectionStringHelper.SetDataSource(settings.ConnectionString, newFilename)
                        : settings.ConnectionString;

                    await InitialiseDatabase(config, connectionString, settings.DBType);

                    using (IDbConnection connection = _dbConnectionFactory.GetDb(connectionString, settings.DBType))
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
            finally
            {
                var removeFile = generateTempFile && File.Exists(newFilename);
                if (removeFile)
                {
                    await _logger.Log($"Removing file {newFilename}");
                    File.Delete(newFilename);
                }
            }
        }

        private async Task UpdateItems(
            IDbConnection connection,
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

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                await _logger.Log("Reading database");
                using (IDataReader reader = command.ExecuteReader())
                {
                    var isOk = _dataChecker.CheckDataIsOK(reader);
                    if (!isOk)
                    {
                        return;
                    }

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
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var headerText = reader.GetName(i);
                        var header = GetHeaderName(headerText);
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
