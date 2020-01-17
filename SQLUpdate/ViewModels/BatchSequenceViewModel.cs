using SCQueryConnect.Commands;
using SCQueryConnect.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace SCQueryConnect.ViewModels
{
    public class BatchSequenceViewModel : IBatchSequenceViewModel
    {
        private const int BatchSequenceIndexMultiplier = 2;

        private ICollectionView _excludedConnections;
        private ICollectionView _includedConnections;
        private IList<QueryData> _connections;
        private QueryData _selectedExcludedConnection;
        private QueryData _selectedIncludedConnection;
        private string _selectedArchitecture;

        public const string ArchitectureAuto = "Auto";
        public const string Architecture64 = "64 bit";
        public const string Architecture32 = "32 bit";

        public event PropertyChangedEventHandler PropertyChanged;

        public ICollectionView ExcludedConnections
        {
            get => _excludedConnections;

            set
            {
                if (_excludedConnections != value)
                {
                    _excludedConnections = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICollectionView IncludedConnections
        {
            get => _includedConnections;

            set
            {
                if (_includedConnections != value)
                {
                    _includedConnections = value;
                    OnPropertyChanged();
                }
            }
        }

        public QueryData SelectedExcludedConnection
        {
            get => _selectedExcludedConnection;

            set
            {
                if (_selectedExcludedConnection != value)
                {
                    _selectedExcludedConnection = value;
                    OnPropertyChanged();
                    RaiseCanExecuteChangedForAllCommands();
                }
            }
        }

        public QueryData SelectedIncludedConnection
        {
            get => _selectedIncludedConnection;

            set
            {
                if (_selectedIncludedConnection != value)
                {
                    _selectedIncludedConnection = value;
                    OnPropertyChanged();
                    RaiseCanExecuteChangedForAllCommands();
                }
            }
        }

        public ActionCommand<QueryData> AddToBatchCommand { get; }
        public ActionCommand<QueryData> RemoveFromBatchCommand { get; }
        public ActionCommand<QueryData> DecreaseBatchIndexCommand { get; }
        public ActionCommand<QueryData> IncreaseBatchIndexCommand { get; }

        public string SelectedArchitecture
        {
            get => _selectedArchitecture;
            
            set
            {
                if (_selectedArchitecture != value)
                {
                    _selectedArchitecture = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] ArchitectureOptions { get; } =
        {
            ArchitectureAuto,
            Architecture64,
            Architecture32
        };

        public BatchSequenceViewModel()
        {
            AddToBatchCommand = new ActionCommand<QueryData>(
                AddToBatch,
                qd => qd != null);

            RemoveFromBatchCommand = new ActionCommand<QueryData>(
                RemoveFromBatch,
                qd => qd != null);

            DecreaseBatchIndexCommand = new ActionCommand<QueryData>(
                DecreaseBatchIndex,
                qd => qd != null && qd.BatchSequenceIndex > 0);

            IncreaseBatchIndexCommand = new ActionCommand<QueryData>(
                IncreaseBatchIndex,
                qd => qd != null &&
                      qd.BatchSequenceIndex / 2 < _connections.Count(c =>
                          c.BatchSequenceIndex > QueryData.DefaultBatchSequenceIndex) - 1);

            SelectedArchitecture = ArchitectureAuto;
        }

        public void SetConnections(IList<QueryData> connections)
        {
            _connections = connections;

            ExcludedConnections = new ListCollectionView((IList) connections)
            {
                Filter = obj => ((QueryData)obj).BatchSequenceIndex <= QueryData.DefaultBatchSequenceIndex
            };

            IncludedConnections = new ListCollectionView((IList) connections)
            {
                Filter = obj => ((QueryData)obj).BatchSequenceIndex > QueryData.DefaultBatchSequenceIndex
            };

            var sort = new SortDescription(
                nameof(QueryData.BatchSequenceIndex),
                ListSortDirection.Ascending);

            ExcludedConnections.SortDescriptions.Add(sort);
            IncludedConnections.SortDescriptions.Add(sort);
        }

        public void AddToBatch(QueryData data)
        {
            var count = _connections.Count(c =>
                c.BatchSequenceIndex > QueryData.DefaultBatchSequenceIndex);

            data.BatchSequenceIndex = count * BatchSequenceIndexMultiplier;
            RefreshCollectionViews();
        }

        public void RemoveFromBatch(QueryData data)
        {
            data.BatchSequenceIndex = QueryData.DefaultBatchSequenceIndex;
            RefreshCollectionViews();
        }

        public void DecreaseBatchIndex(QueryData data)
        {
            var newIndex = data.BatchSequenceIndex - BatchSequenceIndexMultiplier - 1;
            data.BatchSequenceIndex = newIndex;
            RefreshCollectionViews();
            
            DecreaseBatchIndexCommand.RaiseCanExecuteChanged();
            IncreaseBatchIndexCommand.RaiseCanExecuteChanged();
        }

        public void IncreaseBatchIndex(QueryData data)
        {
            var newIndex = data.BatchSequenceIndex + BatchSequenceIndexMultiplier + 1;
            data.BatchSequenceIndex = newIndex;
            RefreshCollectionViews();
            
            DecreaseBatchIndexCommand.RaiseCanExecuteChanged();
            IncreaseBatchIndexCommand.RaiseCanExecuteChanged();
        }

        private void RefreshCollectionViews()
        {
            // Reassign all batch index values
            var ordered = _connections
                .Where(c => c.BatchSequenceIndex > QueryData.DefaultBatchSequenceIndex)
                .OrderBy(c => c.BatchSequenceIndex).ToArray();

            for (var i = ordered.Length - 1; i >= 0; i--)
            {
                var connection = ordered[i];
                connection.BatchSequenceIndex = i * BatchSequenceIndexMultiplier;
            }

            ExcludedConnections.Refresh();
            IncludedConnections.Refresh();
        }

        private void RaiseCanExecuteChangedForAllCommands()
        {
            AddToBatchCommand.RaiseCanExecuteChanged();
            RemoveFromBatchCommand.RaiseCanExecuteChanged();
            DecreaseBatchIndexCommand.RaiseCanExecuteChanged();
            IncreaseBatchIndexCommand.RaiseCanExecuteChanged();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
