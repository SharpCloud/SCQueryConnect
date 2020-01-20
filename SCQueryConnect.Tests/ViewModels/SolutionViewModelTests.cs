using NUnit.Framework;
using SCQueryConnect.ViewModels;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class SolutionViewModelTests
    {
        private const string SolutionName = "SolutionName";

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
                    Solution = SolutionName,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = availableName,
                    Solution = SolutionName
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
                    Name = dataA,
                    Solution = SolutionName
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionName
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
                    Solution = SolutionName,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionName,
                    SolutionIndex = 2
                }
            };

            var vm = new SolutionViewModel();
            vm.SetConnections(data);
            vm.SelectedSolution = SolutionName;

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
                    Solution = SolutionName,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionName,
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
                    Solution = SolutionName,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionName,
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

        [Test]
        public void SettingSelectedSolutionAlsoSetsQueryDataSelectedSolution()
        {
            // Arrange

            var data = new[]
            {
                new QueryData(),
                new QueryData()
            };

            var vm = new SolutionViewModel();
            vm.SetConnections(data);

            // Act

            vm.SelectedSolution = SolutionName;

            // Assert

            var connections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            var allSet = connections.All(c => c.Solution == SolutionName);
            
            Assert.AreEqual(2, connections.Length);
            Assert.IsTrue(allSet);
        }

        [Apartment(ApartmentState.STA)]
        [TestCase("Connections", Visibility.Visible, Visibility.Collapsed)]
        [TestCase("Solutions", Visibility.Collapsed, Visibility.Visible)]
        public void SettingSelectedParentTabSetsVisibilities(
            string tabHeader,
            Visibility expectedConnectionsVisibility,
            Visibility expectedSolutionsVisibility)
        {
            // Arrange

            var vm = new SolutionViewModel();

            // Act

            vm.SelectedParentTab = new TabItem
            {
                Header = tabHeader
            };

            // Assert

            Assert.AreEqual(expectedConnectionsVisibility, vm.ConnectionsVisibility);
            Assert.AreEqual(expectedSolutionsVisibility, vm.SolutionsVisibility);
        }
    }
}
