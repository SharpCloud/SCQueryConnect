using Newtonsoft.Json;
using SCQueryConnect.Commands;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System;
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

        private readonly IConnectionNameValidator _nameValidator;
        private readonly IMessageService _messageService;

        private ICollectionView _excludedConnections;
        private ICollectionView _includedConnections;
        private IList<QueryData> _connections;
        private QueryData _selectedExcludedConnection;
        private QueryData _selectedIncludedConnection;
        private ObservableCollection<Solution> _solutions = new ObservableCollection<Solution>();
        private string _selectedArchitecture;
        private Solution _selectedSolution;
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

        public ObservableCollection<Solution> Solutions
        {
            get => _solutions;

            set
            {
                if (_solutions != value)
                {
                    _solutions = value;
                    OnPropertyChanged();
                }
            }
        }

        public IActionCommand AddNewSolutionCommand { get; }
        public IActionCommand MoveSolutionUpCommand { get; }
        public IActionCommand MoveSolutionDownCommand { get; }
        public IActionCommand CopySolutionCommand { get; }
        public IActionCommand RemoveSolutionCommand { get; }

        public IActionCommand IncludeInSolutionCommand { get; }
        public IActionCommand ExcludeFromSolutionCommand { get; }
        public IActionCommand DecreaseSolutionIndexCommand { get; }
        public IActionCommand IncreaseSolutionIndexCommand { get; }

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

        public Solution SelectedSolution
        {
            get => _selectedSolution;

            set
            {
                if (_selectedSolution != value)
                {
                    if (_selectedSolution != null)
                    {
                        _selectedSolution.PropertyChanged -= SelectedSolutionPropertyChanged;
                    }

                    _selectedSolution = value;
                    OnPropertyChanged();

                    if (_connections != null)
                    {
                        foreach (var connection in _connections)
                        {
                            connection.Solution = value?.Id;
                        }

                        RefreshCollectionViews();
                    }

                    RaiseCanExecuteChangedForAllCommands();

                    if (_selectedSolution != null)
                    {
                        _selectedSolution.PropertyChanged += SelectedSolutionPropertyChanged;
                    }
                }
            }
        }

        private void SelectedSolutionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var args = (ExtendedPropertyChangedEventArgs) e;
            var msg = _nameValidator.Validate((string) args.NewValue);
            var isValid = string.IsNullOrWhiteSpace(msg);
            
            if (!isValid)
            {
                var solution = (Solution) sender;
                solution.Name = (string) args.OldValue;
                _messageService.ShowMessage(msg);
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

        public SolutionViewModel(
            IConnectionNameValidator nameValidator,
            IMessageService messageService)
        {
            _nameValidator = nameValidator;
            _messageService = messageService;

            AddNewSolutionCommand = new ActionCommand<object>(
                obj => AddNewSolution(),
                obj => true);

            MoveSolutionUpCommand = new ActionCommand<Solution>(
                MoveSolutionUp,
                s => Solutions.IndexOf(s) > 0);

            MoveSolutionDownCommand = new ActionCommand<Solution>(
                MoveSolutionDown,
                s => Solutions.IndexOf(s) < Solutions.Count - 1);

            CopySolutionCommand = new ActionCommand<Solution>(
                Copy,
                s => s != null);

            RemoveSolutionCommand = new ActionCommand<Solution>(
                RemoveSolution,
                s => s != null);

            IncludeInSolutionCommand = new ActionCommand<QueryData>(
                IncludeInSolution,
                qd => qd != null);

            ExcludeFromSolutionCommand = new ActionCommand<QueryData>(
                ExcludeFromSolution,
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
                Filter = obj =>
                {
                    var query = obj as QueryData;

                    var included =
                        query?.SolutionIndex != null &&
                        query.SolutionIndex <= QueryData.DefaultSolutionIndex;

                    return included;
                }
            };

            IncludedConnections = new ListCollectionView((IList) connections)
            {
                Filter = obj =>
                {
                    var query = obj as QueryData;

                    var included =
                        query?.SolutionIndex != null &&
                        query.SolutionIndex > QueryData.DefaultSolutionIndex;

                    return included;
                }
            };

            var sort = new SortDescription(
                nameof(QueryData.SolutionIndex),
                ListSortDirection.Ascending);

            ExcludedConnections.SortDescriptions.Add(sort);
            IncludedConnections.SortDescriptions.Add(sort);
        }

        public void IncludeInSolution(QueryData data)
        {
            var count = _connections.Count(c =>
                c.SolutionIndex > QueryData.DefaultSolutionIndex);

            data.SolutionIndex = count * SolutionIndexMultiplier;
            RefreshCollectionViews();
        }

        public void ExcludeFromSolution(QueryData data)
        {
            data.UnsetSolutionIndex(SelectedSolution.Id);
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

        public void AddNewSolution()
        {
            var solution = new Solution();
            Solutions.Add(solution);
            SelectedSolution = solution;
        }

        public void RemoveSolution(Solution solution)
        {
            Solutions.Remove(solution);
            SelectedSolution = null;

            foreach (var connection in _connections)
            {
                connection.UnsetSolutionIndex(solution.Id);
            }
        }

        public void MoveSolutionUp(Solution solution)
        {
            var index = Solutions.IndexOf(solution);
            Solutions.Move(index, index - 1);
            RaiseCanExecuteChangedForAllCommands();
        }

        public void MoveSolutionDown(Solution solution)
        {
            var index = Solutions.IndexOf(solution);
            Solutions.Move(index, index + 1);
            RaiseCanExecuteChangedForAllCommands();
        }

        public void Copy(Solution solution)
        {
            var json = JsonConvert.SerializeObject(solution);
            var copy = JsonConvert.DeserializeObject<Solution>(json);
            copy.Id = Guid.NewGuid().ToString();

            var index = Solutions.IndexOf(solution);
            Solutions.Insert(index + 1, copy);
            RaiseCanExecuteChangedForAllCommands();
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
            AddNewSolutionCommand.RaiseCanExecuteChanged();
            MoveSolutionUpCommand.RaiseCanExecuteChanged();
            MoveSolutionDownCommand.RaiseCanExecuteChanged();
            CopySolutionCommand.RaiseCanExecuteChanged();
            RemoveSolutionCommand.RaiseCanExecuteChanged();

            IncludeInSolutionCommand.RaiseCanExecuteChanged();
            ExcludeFromSolutionCommand.RaiseCanExecuteChanged();
            DecreaseSolutionIndexCommand.RaiseCanExecuteChanged();
            IncreaseSolutionIndexCommand.RaiseCanExecuteChanged();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
