using SCQueryConnect.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        private TabItem _selectedParentTab;
        private Visibility _connectionsVisibility;
        private Visibility _solutionsVisibility;

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
