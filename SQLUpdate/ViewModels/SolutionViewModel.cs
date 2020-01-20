using SCQueryConnect.Commands;
using SCQueryConnect.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SCQueryConnect.ViewModels
{
    public class SolutionViewModel : ISolutionViewModel
    {
        private const int SolutionIndexMultiplier = 2;

        private ICollectionView _excludedConnections;
        private ICollectionView _includedConnections;
        private IList<QueryData> _connections;
        private QueryData _selectedExcludedConnection;
        private QueryData _selectedIncludedConnection;
        private string _selectedArchitecture;
        private string _selectedSolution;
        private TabItem _selectedParentTab;
        private Visibility _connectionsVisibility;
        private Visibility _solutionsVisibility;

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

        public ObservableCollection<string> Solutions { get; } = new ObservableCollection<string>();

        public ActionCommand<QueryData> AddToSolutionCommand { get; }
        public ActionCommand<QueryData> RemoveFromSolutionCommand { get; }
        public ActionCommand<QueryData> DecreaseSolutionIndexCommand { get; }
        public ActionCommand<QueryData> IncreaseSolutionIndexCommand { get; }

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

        public string SelectedSolution
        {
            get => _selectedSolution;

            set
            {
                if (_selectedSolution != value)
                {
                    _selectedSolution = value;
                    OnPropertyChanged();

                    foreach (var connection in _connections)
                    {
                        connection.Solution = value;
                    }
                }
            }
        }

        public TabItem SelectedParentTab
        {
            get => _selectedParentTab;

            set
            {
                if (_selectedParentTab != value)
                {
                    _selectedParentTab = value;
                    OnPropertyChanged();

                    switch (_selectedParentTab.Header)
                    {
                        case "Connections":
                            ConnectionsVisibility = Visibility.Visible;
                            SolutionsVisibility = Visibility.Collapsed;
                            break;

                        case "Solutions":
                            ConnectionsVisibility = Visibility.Collapsed;
                            SolutionsVisibility = Visibility.Visible;
                            break;
                    }
                }
            }
        }

        public Visibility ConnectionsVisibility
        {
            get => _connectionsVisibility;

            set
            {
                if (_connectionsVisibility != value)
                {
                    _connectionsVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility SolutionsVisibility
        {
            get => _solutionsVisibility;

            set
            {
                if (_solutionsVisibility != value)
                {
                    _solutionsVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public SolutionViewModel()
        {
            AddToSolutionCommand = new ActionCommand<QueryData>(
                AddToSolution,
                qd => qd != null);

            RemoveFromSolutionCommand = new ActionCommand<QueryData>(
                RemoveFromSolution,
                qd => qd != null);

            DecreaseSolutionIndexCommand = new ActionCommand<QueryData>(
                DecreaseSolutionIndex,
                qd => qd != null && qd.SolutionIndex > 0);

            IncreaseSolutionIndexCommand = new ActionCommand<QueryData>(
                IncreaseSolutionIndex,
                qd => qd != null &&
                      qd.SolutionIndex / 2 < _connections.Count(c =>
                          c.SolutionIndex > QueryData.DefaultSolutionIndex) - 1);

            SelectedArchitecture = ArchitectureAuto;
        }

        public void SetConnections(IList<QueryData> connections)
        {
            _connections = connections;

            ExcludedConnections = new ListCollectionView((IList) connections)
            {
                Filter = obj => ((QueryData)obj).SolutionIndex <= QueryData.DefaultSolutionIndex
            };

            IncludedConnections = new ListCollectionView((IList) connections)
            {
                Filter = obj => ((QueryData)obj).SolutionIndex > QueryData.DefaultSolutionIndex
            };

            var sort = new SortDescription(
                nameof(QueryData.SolutionIndex),
                ListSortDirection.Ascending);

            ExcludedConnections.SortDescriptions.Add(sort);
            IncludedConnections.SortDescriptions.Add(sort);
        }

        public void AddToSolution(QueryData data)
        {
            var count = _connections.Count(c =>
                c.SolutionIndex > QueryData.DefaultSolutionIndex);

            data.SolutionIndex = count * SolutionIndexMultiplier;
            RefreshCollectionViews();
        }

        public void RemoveFromSolution(QueryData data)
        {
            data.UnsetSolutionIndex(SelectedSolution);
            RefreshCollectionViews();
        }

        public void DecreaseSolutionIndex(QueryData data)
        {
            var newIndex = data.SolutionIndex - SolutionIndexMultiplier - 1;
            data.SolutionIndex = newIndex;
            RefreshCollectionViews();
            
            DecreaseSolutionIndexCommand.RaiseCanExecuteChanged();
            IncreaseSolutionIndexCommand.RaiseCanExecuteChanged();
        }

        public void IncreaseSolutionIndex(QueryData data)
        {
            var newIndex = data.SolutionIndex + SolutionIndexMultiplier + 1;
            data.SolutionIndex = newIndex;
            RefreshCollectionViews();
            
            DecreaseSolutionIndexCommand.RaiseCanExecuteChanged();
            IncreaseSolutionIndexCommand.RaiseCanExecuteChanged();
        }

        private void RefreshCollectionViews()
        {
            // Reassign all solution index values
            var ordered = _connections
                .Where(c => c.SolutionIndex > QueryData.DefaultSolutionIndex)
                .OrderBy(c => c.SolutionIndex).ToArray();

            for (var i = ordered.Length - 1; i >= 0; i--)
            {
                var connection = ordered[i];
                connection.SolutionIndex = i * SolutionIndexMultiplier;
            }

            ExcludedConnections.Refresh();
            IncludedConnections.Refresh();
        }

        private void RaiseCanExecuteChangedForAllCommands()
        {
            AddToSolutionCommand.RaiseCanExecuteChanged();
            RemoveFromSolutionCommand.RaiseCanExecuteChanged();
            DecreaseSolutionIndexCommand.RaiseCanExecuteChanged();
            IncreaseSolutionIndexCommand.RaiseCanExecuteChanged();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
