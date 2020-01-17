using SCQueryConnect.Commands;
using System.Collections.Generic;
using System.ComponentModel;

namespace SCQueryConnect.Interfaces
{
    public interface IBatchSequenceViewModel : INotifyPropertyChanged
    {
        ICollectionView ExcludedConnections { get; }
        ICollectionView IncludedConnections { get; }

        ActionCommand<QueryData> AddToBatchCommand { get; }
        ActionCommand<QueryData> RemoveFromBatchCommand { get; }
        ActionCommand<QueryData> DecreaseBatchIndexCommand { get; }
        ActionCommand<QueryData> IncreaseBatchIndexCommand { get; }

        QueryData SelectedExcludedConnection { get; set; }
        QueryData SelectedIncludedConnection { get; set; }

        string SelectedArchitecture { get; set; }
        string[] ArchitectureOptions { get; }

        void SetConnections(IList<QueryData> connections);
    }
}
