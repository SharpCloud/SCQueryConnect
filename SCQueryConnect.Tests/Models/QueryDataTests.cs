using NUnit.Framework;
using SCQueryConnect.Models;
using System.Collections.Generic;

namespace SCQueryConnect.Tests.Models
{
    [TestFixture]
    public class QueryDataTests
    {
        [Test]
        public void SettingExistingSolutionIndexToNullRemovesSolutionIndexesEntry()
        {
            // Arrange

            const string solutionId = "SolutionID";

            var query = new QueryData
            {
                Solution = solutionId,
                SolutionIndexes = new Dictionary<string, int>
                {
                    [solutionId] = 39
                }
            };

            // Act

            query.SolutionIndex = null;

            // Assert

            Assert.IsEmpty(query.SolutionIndexes);
        }

        [Test]
        public void SettingNewSolutionIndexToNullRemovesSolutionIndexesEntry()
        {
            // Arrange

            const string solutionId = "SolutionID";

            var query = new QueryData
            {
                Solution = solutionId,
                SolutionIndexes = new Dictionary<string, int>()
            };

            // Act

            query.SolutionIndex = null;

            // Assert

            Assert.IsEmpty(query.SolutionIndexes);
        }
    }
}
