using SC.API.ComInterop.ArrayProcessing;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Helpers.DataValidation;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
using SCQueryConnect.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PanelType = SC.API.ComInterop.Models.Panel.PanelType;

namespace SCQueryConnect.Common.Helpers
{
    public class QueryConnectHelper : IQueryConnectHelper
    {
        public const string MsAdeUrl = "https://www.microsoft.com/en-gb/download/details.aspx?id=13255";

        private readonly IDictionary<PanelType, DefaultPanelValueSet> _defaultPanelValues =
            new Dictionary<PanelType, DefaultPanelValueSet>
            {
                [PanelType.RichText] = new DefaultPanelValueSet
                {
                    Title = "Rich Text Panel",
                    Data = "<html><head><style type=\"text/css\">.c0 { margin: 0px 0px 10px } .c1 { font-family: \"Arial\" } </style></head><body><p class=\"c0\"><span class=\"c1\">​</span></p></body></html>"
                },
                [PanelType.CustomResource] = new DefaultPanelValueSet
                {
                    Title = "Custom Resources",
                    Data = "{\"Text\":\"\",\"ResLinks\":[]}"
                },
                [PanelType.Image] = new DefaultPanelValueSet
                {
                    Title = "Image Panel",
                    Data = "[]"
                },
                [PanelType.Video] = new DefaultPanelValueSet
                {
                    Title = "Video Panel",
                    Data = "novideoset"
                },
                [PanelType.Attribute] = new DefaultPanelValueSet
                {
                    Title = "Attributes",
                    Data = "[]"
                },
                [PanelType.HTML] = new DefaultPanelValueSet
                {
                    Title = "HTML Panel",
                    Data = ""
                }
            };

        private readonly IArchitectureDetector _architectureDetector;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDictionary<QueryEntityType, IDataChecker> _dataCheckers;
        private readonly IExcelWriter _excelWriter;
        private readonly ILog _logger;
        private readonly IRelationshipsBuilder _relationshipsBuilder;
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
            IEnumerable<IDataChecker> dataCheckers,
            IDbConnectionFactory dbConnectionFactory,
            IExcelWriter excelWriter,
            ILog log,
            IRelationshipsBuilder relationshipsBuilder,
            ISharpCloudApiFactory sharpCloudApiFactory)
        {
            _architectureDetector = architectureDetector;
            _connectionStringHelper = connectionStringHelper;
            _dbConnectionFactory = dbConnectionFactory;
            _excelWriter = excelWriter;
            _logger = log;
            _relationshipsBuilder = relationshipsBuilder;
            _sharpCloudApiFactory = sharpCloudApiFactory;

            _dataCheckers = dataCheckers.ToDictionary(c => c.TargetEntity, c => c);
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
                    var relationshipsValid = await _dataCheckers[QueryEntityType.Relationships]
                        .CheckData(reader);
                    
                    if (!relationshipsValid)
                    {
                        await _logger.LogError("Invalid SQL");
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
            var dataKey = string.Empty;

            switch (dbType)
            {
                case DatabaseType.SharpCloudExcel:
                    dataKey = DatabaseStrings.ExcelFileKey;
                    break;
                case DatabaseType.MsAdeSharpCloudExcel:
                    dataKey = DatabaseStrings.DataSourceKey;
                    break;

                // Other DB types do not need initialisation
            }

            if (!string.IsNullOrWhiteSpace(dataKey))
            {
                var filename = _connectionStringHelper.GetVariable(connectionString, dataKey);
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
            UpdateSettings settings,
            CancellationToken ct)
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
                await _logger.Log("Connecting to SharpCloud " + config.Url);

                var story = sc.LoadStory(settings.TargetStoryId);
                ct.ThrowIfCancellationRequested();

                var isValid = Validate(story, out var message);
                await _logger.Log(message);

                if (isValid)
                {
                    var key = settings.DBType == DatabaseType.Excel
                        ? DatabaseStrings.ExcelFileKey
                        : DatabaseStrings.DataSourceKey;

                    var filename = _connectionStringHelper.GetVariable(
                        settings.ConnectionString,
                        key);

                    var withoutExt = Path.GetFileNameWithoutExtension(filename);
                    generateTempFile = string.IsNullOrWhiteSpace(withoutExt);
                    var pathHelper = new PathHelper();
                    newFilename = pathHelper.GetAbsolutePath($"{Guid.NewGuid()}.xlsx");

                    var connectionString = generateTempFile
                        ? _connectionStringHelper.SetDataSource(settings.ConnectionString, newFilename)
                        : settings.ConnectionString;

                    await InitialiseDatabase(config, connectionString, settings.DBType);
                    ct.ThrowIfCancellationRequested();

                    using (IDbConnection connection = _dbConnectionFactory.GetDb(connectionString, settings.DBType))
                    {
                        connection.Open();

                        await UpdateItems(
                            connection,
                            story,
                            settings.QueryString,
                            settings.MaxRowCount,
                            settings.UnpublishItems);

                        ct.ThrowIfCancellationRequested();

                        await UpdateRelationships(connection, story, settings.QueryStringRels);
                        ct.ThrowIfCancellationRequested();

                        await GetResourceUrlMetadata(connection, settings.QueryStringResourceUrls, story);
                        ct.ThrowIfCancellationRequested();

                        await GetPanelMetadata(connection, settings.QueryStringPanels, story);
                        ct.ThrowIfCancellationRequested();

                        if (settings.BuildRelationships)
                        {
                            await _logger.Log("Building relationships...");

                            await _relationshipsBuilder.AddRelationshipsToStory(story);
                            ct.ThrowIfCancellationRequested();

                            await _logger.Log("Relationships built");
                        }

                        await _logger.Log("Saving Changes");
                        story.Save();
                        await _logger.Log("Save Complete!");
                        await _logger.Log(
                            $"Update process completed in {(DateTime.Now - start).TotalSeconds:f2} seconds");
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
                    $"You can find this at {MsAdeUrl}";

                await _logger.LogError(
                    $"{message}{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarning("Cancelling story update...");
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
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
                    var itemsValid = await _dataCheckers[QueryEntityType.Items]
                        .CheckData(reader);

                    if (!itemsValid)
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

                    // create our string array
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

                    await _logger.Log(
                        $"Processing {row.ToString()} rows: unmatched items " +
                        $"will {(unpublishItems ? "" : "NOT ")}be unpublished");

                    // pass the array to SharpCloud
                    string errorMessage;
                    if (unpublishItems)
                    {
                        if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage, out var updatedItems))
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
                                        await _logger.LogError(ex.Message);
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
                        if (!story.UpdateStoryWithArray(arrayValues, false, out errorMessage, out _))
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

        private async Task GetResourceUrlMetadata(
            IDbConnection connection,
            string sqlString,
            Story story)
        {
            Task<ResourceUrlMetadata> Mapper(Func<string, string> fieldExtractor)
            {
                var metadata = new ResourceUrlMetadata
                {
                    Description = fieldExtractor(ResourceUrlsDataChecker.DescriptionHeader),
                    ItemExternalId = fieldExtractor(ResourceUrlsDataChecker.ExternalIdHeader),
                    Name = fieldExtractor(ResourceUrlsDataChecker.ResourceNameHeader),
                    Url = fieldExtractor(ResourceUrlsDataChecker.UrlHeader)
                };

                return Task.FromResult(metadata);
            }

            var resourceUrlMetadata = await GetMetadata(
                connection,
                sqlString,
                ResourceUrlsDataChecker.RequiredHeadings,
                "Resource URL",
                _dataCheckers[QueryEntityType.ResourceUrls],
                Mapper);

            foreach (var m in resourceUrlMetadata)
            {
                var item = story.Item_FindByExternalId(m.ItemExternalId);

                if (item != null)
                {
                    var existing = item.Resource_FindByName(m.Name);

                    if (existing == null)
                    {
                        item.Resource_AddName(m.Name, m.Description, m.Url);
                    }
                    else
                    {
                        existing.Description = m.Description;
                        existing.Url = new Uri(m.Url);
                    }
                }
                else
                {
                    await _logger.LogWarning($"Cannot find item with external ID: {m.ItemExternalId}");
                }
            }
        }

        private async Task GetPanelMetadata(
            IDbConnection connection,
            string sqlString,
            Story story)
        {
            async Task<PanelMetadata> Mapper(Func<string, string> fieldExtractor)
            {
                PanelMetadata metadata = null;
                var panelTypeString = fieldExtractor(PanelsDataChecker.PanelTypeHeader);

                var success = Enum.TryParse(
                    panelTypeString,
                    true,
                    out PanelType panelType);

                if (success)
                {
                    var title = fieldExtractor(PanelsDataChecker.TitleHeader);
                    var data = fieldExtractor(PanelsDataChecker.DataHeader);

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = _defaultPanelValues[panelType].Title;
                    }
                    
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        data = _defaultPanelValues[panelType].Data;
                    }

                    metadata = new PanelMetadata
                    {
                        Title = title,
                        ItemExternalId = fieldExtractor(PanelsDataChecker.ExternalIdHeader),
                        PanelType = panelType,
                        Data = data
                    };
                }
                else
                {
                    await _logger.LogWarning(
                        $"Unrecognized panel type '{panelTypeString}' ignored. Valid values are [{PanelTypeHelper.ValidTypes}]");
                }

                return metadata;
            }

            var panelMetadata = await GetMetadata(
                connection,
                sqlString,
                PanelsDataChecker.RequiredHeadings,
                "Panel",
                _dataCheckers[QueryEntityType.Panels],
                Mapper);

            foreach (var m in panelMetadata)
            {
                var item = story.Item_FindByExternalId(m.ItemExternalId);

                if (item == null)
                {
                    await _logger.LogWarning($"Cannot find item with external ID: {m.ItemExternalId}");
                    continue;
                }

                var addPanel = false;
                var existing = item.Panel_FindByTitle(m.Title);

                if (existing == null)
                {
                    addPanel = true;
                }
                else
                {
                    if (existing.Type == m.PanelType)
                    {
                        existing.Data = m.Data;
                    }
                    else
                    {
                        addPanel = true;
                        item.Panel_DeleteByTitle(m.Title);
                    }
                }

                if (addPanel)
                {
                    item.Panel_Add(m.Title, m.PanelType, m.Data);
                }
            }
        }

        private async Task<IList<T>> GetMetadata<T>(
            IDbConnection connection,
            string sqlString,
            HashSet<string> columnHeadings,
            string description,
            IDataChecker dataChecker,
            Func<Func<string, string>, Task<T>> mapper)
            where T : new()
        {
            var metadataList = new List<T>();

            if (string.IsNullOrWhiteSpace(sqlString))
            {
                await _logger.Log($"No {description} Query detected");
                return metadataList;
            }

            await _logger.Log($"Processing {description} Query...");
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sqlString;
                command.CommandType = CommandType.Text;

                await _logger.Log("Reading database");
                using (var reader = command.ExecuteReader())
                {
                    var isValid = await dataChecker.CheckData(reader);
                    if (!isValid)
                    {
                        return metadataList;
                    }

                    var indexes = columnHeadings.ToDictionary(
                        k => k,
                        k => -1,
                        StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        try
                        {
                            var headerText = reader.GetName(i);

                            if (indexes.ContainsKey(headerText))
                            {
                                indexes[headerText] = i;
                            }
                        }
                        catch (Exception)
                        {
                            await _logger.Log($"Could not read {description} header column {i}");
                            throw;
                        }
                    }

                    while (reader.Read())
                    {
                        var objects = new object[reader.FieldCount];
                        reader.GetValues(objects);

                        var metadata = await mapper(heading =>
                            objects[indexes[heading]].ToString());

                        if (metadata != null)
                        {
                            metadataList.Add(metadata);
                        }
                    }
                }
            }

            await _logger.Log($"Processed {description} Query: found {metadataList.Count} items");
            return metadataList;
        }
    }
}
