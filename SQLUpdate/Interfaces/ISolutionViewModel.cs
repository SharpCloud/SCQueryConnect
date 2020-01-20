using SCQueryConnect.Commands;
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

        ActionCommand<QueryData> AddToSolutionCommand { get; }
        ActionCommand<QueryData> RemoveFromSolutionCommand { get; }
        ActionCommand<QueryData> DecreaseSolutionIndexCommand { get; }
        ActionCommand<QueryData> IncreaseSolutionIndexCommand { get; }

        QueryData SelectedExcludedConnection { get; set; }
        QueryData SelectedIncludedConnection { get; set; }

        ObservableCollection<string> Solutions { get; }

        string SelectedArchitecture { get; set; }
        string[] ArchitectureOptions { get; }
        string SelectedSolution { get; set; }

        TabItem SelectedParentTab { get; set; }
        Visibility ConnectionsVisibility { get; set; }
        Visibility SolutionsVisibility { get; set; }

        void SetConnections(IList<QueryData> connections);
    }
}
