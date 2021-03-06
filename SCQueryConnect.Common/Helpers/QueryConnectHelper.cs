﻿using SC.API.ComInterop.ArrayProcessing;
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
    public class QueryConnectHelper : IQueryConnectHelper
    {
        private readonly IArchitectureDetector _architectureDetector;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IDataChecker _dataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IExcelWriter _excelWriter;
        private readonly ILog _logger;
        private readonly IRelationshipsDataChecker _relationshipsDataChecker;
        private readonly ISharpCloudApiFactory _sharpCloudApiFactory;
        private readonly Regex _tagHeaderRegex = new Regex(Regex.Escape("#"));

        public string AppNameOnly => $"SharpCloud QueryConnect v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public string AppName
        {
            get
            {
                return _architectureDetector.Is32Bit
                    ? $"{AppNameOnly} - 32Bit(x86)"
                    : $"{AppNameOnly} - 64Bit(AnyCPU)";
            }
        }

        public QueryConnectHelper(
            IArchitectureDetector architectureDetector,
            IConnectionStringHelper connectionStringHelper,
            IDataChecker dataChecker,
            IDbConnectionFactory dbConnectionFactory,
            IExcelWriter excelWriter,
            ILog log,
            IRelationshipsDataChecker relationshipsDataChecker,
            ISharpCloudApiFactory sharpCloudApiFactory)
        {
            _architectureDetector = architectureDetector;
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
                        await LogError("Invalid SQL");
                        return;
                    }

                    // Write array column headers

                    columnCount = reader.FieldCount;
                    for (int i = 0; i < columnCount; i++)
                    {
                        try
                        {
                            dataList.Add(new string[columnCount]);

                            var headerText = reader.GetName(i);
                            var header = GetHeaderName(headerText);
                            dataList[0][i] = header;
                        }
                        catch (Exception)
                        {
                            await _logger.Log($"Could not read relationship header column {i}");
                            throw;
                        }
                    }

                    // Write array data

                    while (reader.Read())
                    {
                        var dataRow = new string[columnCount];
                        dataList.Add(dataRow);

                        for (int i = 0; i < columnCount; i++)
                        {
                            try
                            {
                                dataRow[i] = reader[i].ToString();
                            }
                            catch (Exception)
                            {
                                var dataSoFar = string.Join(", ", dataRow);
                                await _logger.Log($"Could not read relationship data column {i}. Data successfully read: [{dataSoFar}]");
                                throw;
                            }
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
                var result = updater.UpdateRelationships(data, story);

                var hasMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage);
                if (hasMessage)
                {
                    await _logger.Log(result.ErrorMessage);
                }
            }

            await _logger.Log($"{rowCount} relationships processed.");
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

                await _logger.Log($"{AppName}: Starting update process...");
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
            catch (InvalidOperationException ex) when (ex.Message.Contains(Constants.AccessDBEngineErrorMessage))
            {
                var altArchitecture = _architectureDetector.Is32Bit ? "64" : "32";

                var message =
                    $"Something went wrong connecting to the data source. Here are some things that might help:{Environment.NewLine}" +
                    $"  * Please download and install the {altArchitecture} bit version of Query Connect.{Environment.NewLine}" +
                    $"  * If that doesn't fix the problem, try installing the Microsoft Access Database Engine 2010 Redistributable. " +
                    $"You can find this by clicking on the 'Download tools for Excel/Access' link on the 'About' tab in Query Connect.";

                await LogError($"{message}{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                await LogError(ex.Message + Environment.NewLine + ex.StackTrace);
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
                                        await LogError(ex.Message);
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

        private async Task LogError(string message)
        {
            await _logger.Log($"ERROR: {message}");
        }
    }
}
