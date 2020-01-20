using NUnit.Framework;
using SCQueryConnect.ViewModels;
using System.Linq;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class SolutionViewModelTests
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
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = availableName
                }
            };

            var vm = new SolutionViewModel();

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

            var vm = new SolutionViewModel();
            vm.SetConnections(data);

            // Act

            vm.AddToSolution(data[1]);
            vm.AddToSolution(data[0]);

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
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    SolutionIndex = 2
                }
            };

            var vm = new SolutionViewModel();
            vm.SetConnections(data);

            // Act

            vm.RemoveFromSolution(data[0]);

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
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    SolutionIndex = 2
                }
            };

            var vm = new SolutionViewModel();
            vm.SetConnections(data);

            // Act

            vm.DecreaseSolutionIndex(data[1]);

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
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    SolutionIndex = 2
                }
            };

            var vm = new SolutionViewModel();
            vm.SetConnections(data);

            // Act

            vm.IncreaseSolutionIndex(data[0]);

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
