using SCQueryConnect.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SCQueryConnect.Interfaces
{
    public interface ISolutionViewModel : INotifyPropertyChanged
    {
        ICollectionView ExcludedConnections { get; }
        ICollectionView IncludedConnections { get; }

        IActionCommand AddNewSolutionCommand { get; }
        IActionCommand MoveSolutionUpCommand { get; }
        IActionCommand MoveSolutionDownCommand { get; }
        IActionCommand CopySolutionCommand { get; }
        IActionCommand RemoveSolutionCommand { get; }

        IActionCommand IncludeInSolutionCommand { get; }
        IActionCommand ExcludeFromSolutionCommand { get; }
        IActionCommand MoveConnectionUp { get; }
        IActionCommand MoveConnectionDown { get; }

        QueryData SelectedExcludedConnection { get; set; }
        QueryData SelectedIncludedConnection { get; set; }

        ObservableCollection<Solution> Solutions { get; set; }
        Solution SelectedSolution { get; set; }

        void SetConnections(IList<QueryData> connections);
    }
}
