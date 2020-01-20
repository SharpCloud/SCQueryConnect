using SCQueryConnect.Commands;
using System.Collections.Generic;
using System.ComponentModel;

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

        string SelectedArchitecture { get; set; }
        string[] ArchitectureOptions { get; }

        void SetConnections(IList<QueryData> connections);
    }
}
