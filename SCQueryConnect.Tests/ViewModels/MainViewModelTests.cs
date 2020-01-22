using NUnit.Framework;
using SCQueryConnect.ViewModels;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Tests.ViewModels
{
    [TestFixture]
    public class MainViewModelTests
    {
        [Apartment(ApartmentState.STA)]
        [TestCase("Connections", Visibility.Visible, Visibility.Collapsed)]
        [TestCase("Solutions", Visibility.Collapsed, Visibility.Visible)]
        public void SettingSelectedParentTabSetsVisibilities(
            string tabHeader,
            Visibility expectedConnectionsVisibility,
            Visibility expectedSolutionsVisibility)
        {
            // Arrange

            var vm = new MainViewModel();

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
