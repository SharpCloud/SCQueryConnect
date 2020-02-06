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
using Attribute = SC.API.ComInterop.Models.Attribute;

namespace SCQueryConnect.Common.Helpers
{
    public class QueryConnectHelper : IQueryConnectHelper
    {
        private readonly IArchitectureDetector _architectureDetector;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IItemDataChecker _itemDataChecker;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IExcelWriter _excelWriter;
        private readonly ILog _logger;
        private readonly IPanelsDataChecker _panelsDataChecker;
        private readonly IRelationshipsDataChecker _relationshipsDataChecker;
        private readonly IResourceUrlDataChecker _resourceUrlDataChecker;
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
            IItemDataChecker itemDataChecker,
            IDbConnectionFactory dbConnectionFactory,
            IExcelWriter excelWriter,
            ILog log,
            IPanelsDataChecker panelsDataChecker,
            IRelationshipsDataChecker relationshipsDataChecker,
            IResourceUrlDataChecker resourceUrlDataChecker,
            ISharpCloudApiFactory sharpCloudApiFactory)
        {
            _architectureDetector = architectureDetector;
            _connectionStringHelper = connectionStringHelper;
            _itemDataChecker = itemDataChecker;
            _dbConnectionFactory = dbConnectionFactory;
            _excelWriter = excelWriter;
            _logger = log;
            _panelsDataChecker = panelsDataChecker;
            _relationshipsDataChecker = relationshipsDataChecker;
            _resourceUrlDataChecker = resourceUrlDataChecker;
            _sharpCloudApiFactory = sharpCloudApiFactory;
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
                    if (!_relationshipsDataChecker.CheckData(reader))
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
                        await GetResourceUrlMetadata(connection, settings.QueryStringResourceUrls, story);
                        await GetPanelMetadata(connection, settings.QueryStringPanels, story);

                        if (settings.BuildRelationships)
                        {
                            await AddRelationshipsToStory(story);
                        }

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

                await _logger.LogError($"{message}{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
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

        private async Task AddRelationshipsToStory(Story story, char separator = ';', bool hasRelValue = false)
        {
            var relAttrib = story.RelationshipAttribute_FindByName("Strength");
            if (hasRelValue && relAttrib == null)
                relAttrib = story.RelationshipAttribute_Add("Strength", RelationshipAttribute.RelationshipAttributeType.Numeric);

            var attribsToTest = new List<Attribute>();

            foreach (var a in story.Attributes)
            {
                if (a.Type == Attribute.AttributeType.Text &&
                    (a.Name.ToLower().Contains("related_") || a.Name == "RelatedItems"))
                {
                    attribsToTest.Add(a);
                }
            }

            if (attribsToTest.Count == 0)
            {
                await _logger.LogWarning("No Related attributes detected.");
                await _logger.LogWarning("Make sure you are using a text attribute called 'Related_<CategoryName>' or just 'RelatedItems'");
                return;
            }

            foreach (var at in attribsToTest)
            {
                bool any = (at.Name == "RelatedItems");
                var catName = at.Name.Substring(8);

                foreach (var i in story.Items)
                {
                    var text = i.GetAttributeValueAsText(at);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var rels = text.Split(separator);
                        foreach (var r in rels)
                        {
                            var Id = r.Trim();
                            var val = r.Substring(r.Length - 1);
                            double num = 0;
                            if (hasRelValue && double.TryParse(val, out num))
                            {
                                num = double.Parse(val);
                                Id = r.Substring(0, Id.Length - 1).Trim();
                            }

                            var i2 = FindItem(story, Id, catName, any);
                            if (i2 != null)
                            {
                                if (story.Relationship_FindByItems(i, i2) == null)
                                {
                                    var rel = story.Relationship_AddNew(i, i2, $"Added from {i.Name}.{at.Name}");
                                    await _logger.Log($"Adding realtionship between '{i.Name}' and '{i2.Name}'");
                                    if (hasRelValue && num > 0)
                                        rel.SetAttributeValue(relAttrib, num);
                                }
                            }
                            else
                            {
                                await _logger.Log($"Could not find item '{Id}' for '{i.Name}.{at.Name}' from 'text'");
                            }
                        }
                    }
                }
            }
        }

        private static Item FindItem(Story story, string extId, string catName, bool any)
        {
            if (any)
                return story.Item_FindByExternalId(extId);

            foreach (var i in story.Items)
            {
                if (i.Category.Name == catName && i.ExternalId == extId)
                    return i;
            }
            return null;// none found
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
                    var isOk = _itemDataChecker.CheckData(reader);
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

                    await _logger.Log($"Processing {row.ToString()} rows");

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
                    Description = fieldExtractor(ResourceUrlDataChecker.DescriptionHeader),
                    ItemExternalId = fieldExtractor(ResourceUrlDataChecker.ExternalIdHeader),
                    Name = fieldExtractor(ResourceUrlDataChecker.ResourceNameHeader),
                    Url = fieldExtractor(ResourceUrlDataChecker.UrlHeader)
                };

                return Task.FromResult(metadata);
            }

            var resourceUrlMetadata = await GetMetadata(
                connection,
                sqlString,
                ResourceUrlDataChecker.RequiredHeadings,
                "Resource URL",
                _resourceUrlDataChecker,
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
                    out Panel.PanelType panelType);

                if (success)
                {
                    metadata = new PanelMetadata
                    {
                        Title = fieldExtractor(PanelsDataChecker.TitleHeader),
                        ItemExternalId = fieldExtractor(PanelsDataChecker.ExternalIdHeader),
                        PanelType = panelType,
                        Data = fieldExtractor(PanelsDataChecker.DataHeader)
                    };
                }
                else
                {

                    var validValues = Enum.GetValues(typeof(Panel.PanelType))
                        .Cast<Panel.PanelType>()
                        .Select(t => t.ToString())
                        .Where(t => string.Compare(t, "Undefined", StringComparison.OrdinalIgnoreCase) != 0);

                    var valid = string.Join(", ", validValues);

                    await _logger.LogWarning(
                        $"Unrecognized panel type '{panelTypeString}' ignored. Valid values are [{valid}]");
                }

                return metadata;
            }

            var panelMetadata = await GetMetadata(
                connection,
                sqlString,
                PanelsDataChecker.RequiredHeadings,
                "Panel",
                _panelsDataChecker,
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
                    var isOk = dataChecker.CheckData(reader);
                    if (!isOk)
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
