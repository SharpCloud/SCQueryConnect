using Moq;
using NUnit.Framework;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class SolutionViewModelTests
    {
        private const string Id1 = "ID1";
        private const string Id2 = "ID2";

        [Test]
        public void ConnectionsAreFiltered()
        {
            // Arrange

            var data = new[]
            {
                new QueryData
                {
                    Id = Id1
                },
                new QueryData
                {
                    Id = Id2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);

            // Act

            vm.SelectedSolution = CreateSolution(Id1);

            // Assert

            var included = GetIncludedConnections(vm).Single();
            Assert.AreEqual(Id1, included.Id);

            var excluded = GetExcludedConnections(vm).Single();
            Assert.AreEqual(Id2, excluded.Id);
        }

        [Test]
        public void ConnectionsAreAddedInCorrectOrder()
        {
            // Arrange

            var data = new[]
            {
                new QueryData
                {
                    Id = Id1
                },
                new QueryData
                {
                    Id = Id2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.SelectedSolution = new Solution();

            // Act

            vm.IncludeInSolution(data[1]);
            vm.IncludeInSolution(data[0]);

            // Assert

            var included = GetIncludedConnections(vm);
            Assert.AreEqual(2, included.Count);
            Assert.AreEqual(Id2, included[0].Id);
            Assert.AreEqual(Id1, included[1].Id);

            var excluded = GetExcludedConnections(vm);
            Assert.IsEmpty(excluded);
        }

        [Test]
        public void ConnectionsAreRemoved()
        {
            // Arrange

            var data = new[]
            {
                new QueryData
                {
                    Id = Id1
                },
                new QueryData
                {
                    Id = Id2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.SelectedSolution = CreateSolution(Id1, Id2);

            // Act

            vm.ExcludeFromSolution(data[0]);

            // Assert

            var included = GetIncludedConnections(vm).Single();
            Assert.AreEqual(Id2, included.Id);

            var excluded = GetExcludedConnections(vm).Single();
            Assert.AreEqual(Id1, excluded.Id);
        }

        [Test]
        public void ConnectionsCanBeMovedUp()
        {
            // Arrange

            var data = new[]
            {
                new QueryData
                {
                    Id = Id1
                },
                new QueryData
                {
                    Id = Id2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.SelectedSolution = CreateSolution(Id1, Id2);

            // Act

            vm.MoveItemUp(Id2, vm.SelectedSolution.ConnectionIds);

            // Assert

            var included = GetIncludedConnections(vm);
            Assert.AreEqual(2, included.Count);
            Assert.AreEqual(Id2, included[0].Id);
            Assert.AreEqual(Id1, included[1].Id);

            var excluded = GetExcludedConnections(vm);
            Assert.IsEmpty(excluded);
        }

        [Test]
        public void ConnectionsCanBeMovedDown()
        {
            // Arrange

            var data = new[]
            {
                new QueryData
                {
                    Id = Id1
                },
                new QueryData
                {
                    Id = Id2
                }
            };

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            vm.SetConnections(data);
            vm.SelectedSolution = CreateSolution(Id1, Id2);

            // Act

            vm.MoveItemDown(Id1, vm.SelectedSolution.ConnectionIds);

            // Assert

            var included = GetIncludedConnections(vm);
            Assert.AreEqual(2, included.Count);
            Assert.AreEqual(Id2, included[0].Id);
            Assert.AreEqual(Id1, included[1].Id);

            var excluded = GetExcludedConnections(vm);
            Assert.IsEmpty(excluded);
        }

        [Test]
        public void AddNewSolutionAddsToSolution()
        {
            // Arrange

            var vm = new SolutionViewModel(
                Mock.Of<IConnectionNameValidator>(),
                Mock.Of<IMessageService>());

            // Act

            vm.AddNewSolution();

            // Assert

            var solution = vm.Solutions.Single();
            Assert.AreEqual(solution, vm.SelectedSolution);
        }

        [Test]
        public void DeleteSolutionRemovesToSolution()
        {
            // Arrange

            var data = new[]
            {
                new QueryData
                {
                    Id = Id1
                },
                new QueryData
                {
                    Id = Id2
                }
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

            vm.MoveItemUp(solution2, vm.Solutions);

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

            vm.MoveItemDown(solution1, vm.Solutions);

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

            var included = GetIncludedConnections(vm);
            Assert.IsEmpty(included);

            var excluded = GetExcludedConnections(vm);
            Assert.IsEmpty(excluded);
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

        private static Solution CreateSolution(params string[] connectionIds)
        {
            return new Solution
            {
                ConnectionIds = new ObservableCollection<string>(connectionIds)
            };
        }

        private static IList<QueryData> GetIncludedConnections(ISolutionViewModel vm)
        {
            return vm.IncludedConnections.Cast<QueryData>().ToArray();
        }

        private static IList<QueryData> GetExcludedConnections(ISolutionViewModel vm)
        {
            return vm.ExcludedConnections.Cast<QueryData>().ToArray();
        }
    }
}
