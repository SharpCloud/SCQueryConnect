using SCQueryConnect.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Interfaces
{
    public interface ISolutionViewModel : INotifyPropertyChanged
    {
        ICollectionView ExcludedConnections { get; }
        ICollectionView IncludedConnections { get; }

        IActionCommand AddNewSolutionCommand { get; }
        IActionCommand RemoveSolutionCommand { get; }

        IActionCommand IncludeInSolutionCommand { get; }
        IActionCommand ExcludeFromSolutionCommand { get; }
        IActionCommand DecreaseSolutionIndexCommand { get; }
        IActionCommand IncreaseSolutionIndexCommand { get; }

        QueryData SelectedExcludedConnection { get; set; }
        QueryData SelectedIncludedConnection { get; set; }

        ObservableCollection<Solution> Solutions { get; }

        string SelectedArchitecture { get; set; }
        string[] ArchitectureOptions { get; }
        Solution SelectedSolution { get; set; }

        TabItem SelectedParentTab { get; set; }
        Visibility ConnectionsVisibility { get; set; }
        Visibility SolutionsVisibility { get; set; }

        void SetConnections(IList<QueryData> connections);
    }
}
