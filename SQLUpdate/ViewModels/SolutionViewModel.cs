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
using System.Windows.Data;

namespace SCQueryConnect.ViewModels
{
    public class SolutionViewModel : ISolutionViewModel
    {
        private readonly IActionCommand[] _allActions;
        private readonly IConnectionNameValidator _nameValidator;
        private readonly IMessageService _messageService;

        private ICollectionView _excludedConnections;
        private ICollectionView _includedConnections;
        private IList<QueryData> _connections = new List<QueryData>();
        private QueryData _selectedExcludedConnection;
        private QueryData _selectedIncludedConnection;
        private ObservableCollection<Solution> _solutions = new ObservableCollection<Solution>();
        private string _selectedArchitecture;
        private Solution _selectedSolution;

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
        public IActionCommand MoveConnectionUp { get; }
        public IActionCommand MoveConnectionDown { get; }

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

                    RefreshCollectionViews();
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
                s => MoveItemUp(s, Solutions),
                s => Solutions.IndexOf(s) > 0);

            MoveSolutionDownCommand = new ActionCommand<Solution>(
                s => MoveItemDown(s, Solutions),
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

            MoveConnectionUp = new ActionCommand<QueryData>(
                qd => MoveItemUp(qd.Id, SelectedSolution.ConnectionIds),
                qd => qd != null && SelectedSolution.ConnectionIds.IndexOf(qd.Id) > 0);

            MoveConnectionDown = new ActionCommand<QueryData>(
                qd => MoveItemDown(qd.Id, SelectedSolution.ConnectionIds),
                qd => qd != null && 
                      SelectedSolution.ConnectionIds.IndexOf(qd.Id) < _connections.Count - 1);

            _allActions = GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(IActionCommand))
                .Select(p => p.GetValue(this))
                .Cast<IActionCommand>()
                .ToArray();

            SelectedArchitecture = ArchitectureAuto;
        }

        public void SetConnections(IList<QueryData> connections)
        {
            _connections = connections;

            ExcludedConnections = new ListCollectionView((IList) connections)
            {
                Filter = obj =>
                    SelectedSolution != null &&
                    !SelectedSolution.ConnectionIds.Contains(((QueryData)obj).Id)
            };

            IncludedConnections = new ListCollectionView((IList)connections)
            {
                Filter = obj =>
                    SelectedSolution != null &&
                    SelectedSolution.ConnectionIds.Contains(((QueryData)obj).Id)
            };

            var sort = new SortDescription(
                nameof(QueryData.DisplayOrder),
                ListSortDirection.Ascending);

            IncludedConnections.SortDescriptions.Add(sort);
        }

        public void IncludeInSolution(QueryData data)
        {
            SelectedSolution.ConnectionIds.Add(data.Id);
            RefreshCollectionViews();
        }

        public void ExcludeFromSolution(QueryData data)
        {
            SelectedSolution.ConnectionIds.Remove(data.Id);
            RefreshCollectionViews();
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
        }

        public void MoveItemUp<T>(T item, ObservableCollection<T> collection)
        {
            var index = collection.IndexOf(item);
            collection.Move(index, index - 1);
            RefreshCollectionViews();
            RaiseCanExecuteChangedForAllCommands();
        }

        public void MoveItemDown<T>(T item, ObservableCollection<T> collection)
        {
            var index = collection.IndexOf(item);
            collection.Move(index, index + 1);
            RefreshCollectionViews();
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
            for (int i = 0; i < SelectedSolution?.ConnectionIds.Count; i++)
            {
                var data = _connections.Single(c => c.Id == SelectedSolution.ConnectionIds[i]);
                data.DisplayOrder = i;
            }

            ExcludedConnections?.Refresh();
            IncludedConnections?.Refresh();
        }

        private void RaiseCanExecuteChangedForAllCommands()
        {
            foreach (var action in _allActions)
            {
                action.RaiseCanExecuteChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
