using SC.API.ComInterop.ArrayProcessing;
using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace SCQueryConnect.Common
{
    public class QueryConnectHelper
    {
        private readonly ILog _logger;
        private readonly IRelationshipsDataChecker _relationshipsDataChecker;

        public QueryConnectHelper(
            ILog log,
            IRelationshipsDataChecker relationshipsDataChecker)
        {
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
    }
}
