using Moq;
using NUnit.Framework;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
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
        private const string SolutionId = "SolutionID";

        [Test]
        public void ConnectionsAreFiltered()
        {
            // Arrange

            const string includedName = "IncludedName";
            const string excludedName = "ExcludedName";

            var data = new[]
            {
                new QueryData
                {
                    Name = includedName,
                    Solution = SolutionId,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = excludedName,
                    Solution = SolutionId
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            // Act

            vm.SetConnections(data);

            // Assert

            var includedConnections = vm.IncludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(1, includedConnections.Length);
            Assert.AreEqual(includedName, includedConnections[0].Name);

            var excludedConnections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            Assert.AreEqual(1, excludedConnections.Length);
            Assert.AreEqual(excludedName, excludedConnections[0].Name);
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
                    Solution = SolutionId
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionId
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);

            // Act

            vm.IncludeInSolution(data[1]);
            vm.IncludeInSolution(data[0]);

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
                    Solution = SolutionId,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionId,
                    SolutionIndex = 2
                }
            };

            var solution = new Solution
            {
                Id = SolutionId
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.SelectedSolution = solution;

            // Act

            vm.ExcludeFromSolution(data[0]);

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
                    Solution = SolutionId,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionId,
                    SolutionIndex = 2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

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
                    Solution = SolutionId,
                    SolutionIndex = 0
                },
                new QueryData
                {
                    Name = dataB,
                    Solution = SolutionId,
                    SolutionIndex = 2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

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

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            
            var solution = new Solution();

            // Act

            vm.SelectedSolution = solution;

            // Assert

            var connections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            var allSet = connections.All(c => c.Solution == solution.Id);
            
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

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            // Act

            vm.SelectedParentTab = new TabItem
            {
                Header = tabHeader
            };

            // Assert

            Assert.AreEqual(expectedConnectionsVisibility, vm.ConnectionsVisibility);
            Assert.AreEqual(expectedSolutionsVisibility, vm.SolutionsVisibility);
        }

        [Test]
        public void AddNewSolutionAddsToSolution()
        {
            // Arrange

            var data = new[]
            {
                new QueryData(),
                new QueryData()
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);

            // Act

            vm.AddNewSolution();

            // Assert

            var solution = vm.Solutions.Single();
            Assert.AreEqual(solution, vm.SelectedSolution);

            var connections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            var allSet = connections.All(c => c.Solution == solution.Id);
            Assert.IsTrue(allSet);
        }

        [Test]
        public void DeleteSolutionRemovesToSolution()
        {
            // Arrange

            var data = new[]
            {
                new QueryData(),
                new QueryData()
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.AddNewSolution();

            var solution = vm.SelectedSolution;

            // Act

            vm.RemoveSolution(solution);

            // Assert

            Assert.IsEmpty(vm.Solutions);
            Assert.IsNull(vm.SelectedSolution);

            var connections = vm.ExcludedConnections.Cast<QueryData>().ToArray();
            var allKeys = connections.SelectMany(c => c.SolutionIndexes.Keys);
            var containsSolutionId = allKeys.Contains(solution.Id);
            Assert.IsFalse(containsSolutionId);
        }

        [Test]
        public void SolutionCanBeMovedUp()
        {
            // Arrange

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.AddNewSolution();
            vm.AddNewSolution();

            var solution1 = vm.Solutions[0];
            var solution2 = vm.Solutions[1];

            // Act

            vm.MoveSolutionUp(solution2);

            // Assert

            Assert.AreEqual(solution2, vm.Solutions[0]);
            Assert.AreEqual(solution1, vm.Solutions[1]);
        }

        [Test]
        public void SolutionCanBeMovedDown()
        {
            // Arrange

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.AddNewSolution();
            vm.AddNewSolution();

            var solution1 = vm.Solutions[0];
            var solution2 = vm.Solutions[1];

            // Act

            vm.MoveSolutionDown(solution1);

            // Assert

            Assert.AreEqual(solution2, vm.Solutions[0]);
            Assert.AreEqual(solution1, vm.Solutions[1]);
        }

        [Test]
        public void SolutionCanBeCopied()
        {
            // Arrange

            const string solutionName = "SolutionName";

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.AddNewSolution();
            vm.SelectedSolution.Name = solutionName;

            // Act

            vm.Copy(vm.SelectedSolution);

            // Assert

            Assert.AreEqual(2, vm.Solutions.Count);
            
            Assert.AreEqual(vm.Solutions[0].Name, vm.Solutions[1].Name);
            Assert.AreNotEqual(vm.Solutions[0].Id, vm.Solutions[1].Id);
        }

        [Test]
        public void ConnectionsListsAreEmptyIfNoSolutionIsSelected()
        {
            // Arrange

            var data = new[]
            {
                new QueryData(),
                new QueryData()
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.AddNewSolution();

            // Act

            vm.SelectedSolution = null;

            // Assert

            var included = vm.IncludedConnections.Cast<QueryData>().Count();
            var excluded = vm.ExcludedConnections.Cast<QueryData>().Count();
            
            Assert.AreEqual(0, included);
            Assert.AreEqual(0, excluded);
        }

        [Test]
        public void SolutionNameCannotBeInvalid()
        {
            // Arrange

            const string invalidMessage = "Invalid";
            const string newName = "NewName";
            const string originalName = "OriginalName";

            var validator = Mock.Of<IConnectionNameValidator>(v =>
                v.Validate(newName) == invalidMessage);

            var messageService = Mock.Of<IMessageService>();

            var vm = new SolutionViewModel(validator, messageService)
            {
                SelectedSolution = new Solution
                {
                    Name = originalName
                }
            };

            // Act

            vm.SelectedSolution.Name = newName;

            // Assert

            Mock.Get(validator).Verify(v =>
                v.Validate(newName));

            Mock.Get(messageService).Verify(s =>
                s.ShowMessage(invalidMessage));

            Assert.AreEqual(originalName, vm.SelectedSolution.Name);
        }

        [Test]
        public void EventHandlersAreRemovedWhenSelectedSolutionIsChanged()
        {
            // Arrange

            const string solutionName1 = "SolutionName1";
            const string solutionName2 = "SolutionName2";

            var validator = Mock.Of<IConnectionNameValidator>();
            var messageService = Mock.Of<IMessageService>();

            var vm = new SolutionViewModel(validator, messageService);

            var solution1 = new Solution();
            var solution2 = new Solution();

            vm.SelectedSolution = solution1;
            vm.SelectedSolution = solution2;

            // Act

            solution1.Name = solutionName1;
            solution2.Name = solutionName2;

            // Assert

            Mock.Get(validator).Verify(v =>
                v.Validate(solutionName1),
                Times.Never);

            Mock.Get(validator).Verify(v =>
                v.Validate(solutionName2),
                Times.Once);
        }
    }
}
