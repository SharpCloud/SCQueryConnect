using NUnit.Framework;
using SCQueryConnect.ViewModels;
using System.Linq;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class BatchSequenceViewModelTests
    {
        [Test]
        public void ConnectionsAreFiltered()
        {
            // Arrange

            const string selectedName = "SelectedName";
            const string availableName = "AvailableName";

            var data = new[]
            {
                new QueryData
                {
                    Name = selectedName,
                    BatchSequenceIndex = 0
                },
                new QueryData
                {
                    Name = availableName
                }
            };

            var vm = new BatchSequenceViewModel();

            // Act

            vm.SetConnections(data);

            // Assert

            var includedConnections = vm.IncludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(1, includedConnections.Length);
            Assert.AreEqual(selectedName, includedConnections[0].Name);

            var excludedConnections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(1, excludedConnections.Length);
            Assert.AreEqual(availableName, excludedConnections[0].Name);
        }

        [Test]
        public void ConnectionsAreAddedInCorrectOrder()
        {
            // Arrange

            const string dataA = "DataA";
            const string dataB = "DataB";

            var data = new[]
            {
                new QueryData
                {
                    Name = dataA
                },
                new QueryData
                {
                    Name = dataB
                }
            };

            var vm = new BatchSequenceViewModel();
            vm.SetConnections(data);

            // Act

            vm.AddToBatch(data[1]);
            vm.AddToBatch(data[0]);

            // Assert

            var includedConnections = vm.IncludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(2, includedConnections.Length);
            Assert.AreEqual(dataB, includedConnections[0].Name);
            Assert.AreEqual(dataA, includedConnections[1].Name);

            var excludedConnections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(0, excludedConnections.Length);
        }

        [Test]
        public void ConnectionsAreRemoved()
        {
            // Arrange

            const string dataA = "DataA";
            const string dataB = "DataB";

            var data = new[]
            {
                new QueryData
                {
                    Name = dataA,
                    BatchSequenceIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    BatchSequenceIndex = 2
                }
            };

            var vm = new BatchSequenceViewModel();
            vm.SetConnections(data);

            // Act

            vm.RemoveFromBatch(data[0]);

            // Assert

            var includedConnections = vm.IncludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(1, includedConnections.Length);
            Assert.AreEqual(dataB, includedConnections[0].Name);

            var excludedConnections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(1, excludedConnections.Length);
            Assert.AreEqual(dataA, excludedConnections[0].Name);
        }

        [Test]
        public void ConnectionsCanBeMovedUp()
        {
            // Arrange

            const string dataA = "DataA";
            const string dataB = "DataB";

            var data = new[]
            {
                new QueryData
                {
                    Name = dataA,
                    BatchSequenceIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    BatchSequenceIndex = 2
                }
            };

            var vm = new BatchSequenceViewModel();
            vm.SetConnections(data);

            // Act

            vm.DecreaseBatchIndex(data[1]);

            // Assert

            var includedConnections = vm.IncludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(2, includedConnections.Length);
            Assert.AreEqual(dataB, includedConnections[0].Name);
            Assert.AreEqual(dataA, includedConnections[1].Name);

            var excludedConnections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(0, excludedConnections.Length);
        }

        [Test]
        public void ConnectionsCanBeMovedDown()
        {
            // Arrange

            const string dataA = "DataA";
            const string dataB = "DataB";

            var data = new[]
            {
                new QueryData
                {
                    Name = dataA,
                    BatchSequenceIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    BatchSequenceIndex = 2
                }
            };

            var vm = new BatchSequenceViewModel();
            vm.SetConnections(data);

            // Act

            vm.IncreaseBatchIndex(data[0]);

            // Assert

            var includedConnections = vm.IncludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(2, includedConnections.Length);
            Assert.AreEqual(dataB, includedConnections[0].Name);
            Assert.AreEqual(dataA, includedConnections[1].Name);

            var excludedConnections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(0, excludedConnections.Length);
        }
    }
}
