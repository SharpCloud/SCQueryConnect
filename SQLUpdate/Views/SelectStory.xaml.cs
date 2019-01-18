using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SQLUpdate.ViewModels;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using CheckBox = System.Windows.Controls.CheckBox;

namespace SQLUpdate.Views
{
    /// <summary>
    /// Interaction logic for SelectStory.xaml
    /// </summary>
    public partial class SelectStory : Window
    {

        private CollectionViewSource storiesCvs;

        private CollectionViewSource teamsCvs;

        private SelectedStoryViewModel _viewModel;

        private string _userName;

        private bool _allowMultiSelect;
        public SelectStory(SharpCloudApi client, bool allowMultiSelect = true, string username = "")
        {
            InitializeComponent();
            
            _viewModel = DataContext as SelectedStoryViewModel;
            _client = client;

            storiesCvs = this.FindResource("storiesCvs") as CollectionViewSource;
            storiesCvs.Source = _viewModel.AllStories;
            teamsCvs = this.FindResource("teamsCvs") as CollectionViewSource;
            teamsCvs.Source = _viewModel.AllTeams;

            Loaded += new RoutedEventHandler(SelectRoadmap_Loaded);

            SelectedIDs = new List<string>();
            SelectedStoryLites = new List<StoryLite>();
            _allowMultiSelect = allowMultiSelect;
            _userName = username.ToLower();
        }
        public List<string> SelectedIDs { get; private set; }
        public List<StoryLite> SelectedStoryLites { get; private set; }

        private SharpCloudApi _client;

 
        void SelectRoadmap_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTeams();
        }

        private void OnClickOK(object sender, RoutedEventArgs e)
        {
            if (!SelectedStoryLites.Any())
                return;

            DialogResult = true;
        }

        private void OnClickCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CollectionViewSource_OnFilter(object sender, FilterEventArgs e)
        {
            var story = (StoryLite)e.Item;
            e.Accepted = MatchOnSearch(story.Name);
        }

        private string _searchStr;
        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tbSearch.Text != "Search stories")
            {
                _searchStr = tbSearch.Text.ToLower();
                storiesCvs.View.Refresh();
            }
        }

        private bool MatchOnSearch(string str)
        {
            if (!string.IsNullOrEmpty(_searchStr))
            {
                str = str.ToLower();
                return str.Contains(_searchStr);
            }
            return true;
        }

        private void LoadTeams()
        {
            _viewModel.AllTeams.Clear();
            var teams = _client.Teams;
            this.teamStoriesContainer.Visibility = Visibility.Visible;
            foreach (Team t in teams)
            {
                _viewModel.AllTeams.Add(t);
            }
            
        }

        private void Team_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Border grid = sender as Border;
            if (grid == null) return;
            var item = grid.DataContext as Team;
            try
            {
                _viewModel.AllStories.Clear();
                var tStories = _client.StoriesTeam(item.Id);

                this.userStoriesContainer.Visibility = Visibility.Visible;
                foreach (StoryLite a in tStories)
                {
                    _viewModel.AllStories.Add(a);
                }

            }
            catch (Exception)
            {
            }
        }

        private CheckBox _checkBox;

        private void Story_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Border grid = sender as Border;
            if (grid == null) return;
            var item = grid.DataContext as StoryLite;
            try
            {
                if (_checkBox != null)
                    _checkBox.IsChecked = !_checkBox.IsChecked;

            }
            catch (Exception)
            {
            }
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var a = sender as Border;
            _checkBox = a.FindName("cbStory") as CheckBox;
        }


        private void Story_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var a = sender as Border;
            a.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 173, 238));
        }

        private void Story_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var a = sender as Border;
            a.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 41, 41, 41));
        }

        private void Team_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var a = sender as Border;
            a.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 41, 41, 41));
        }

        private void Team_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var a = sender as Border;
            a.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 204, 22, 76));
        }

        private void imgIcon_Loaded(object sender, RoutedEventArgs e)
        {
            var t = sender as System.Windows.Controls.Image;
            var content = t.DataContext as SC.API.ComInterop.Models.Team;
            String stringPath = _client.BaseUri + "/image/" + content.Id + "?t=teamid";
            Uri imageUri = new Uri(stringPath, UriKind.Absolute);
            BitmapImage imageBitmap = new BitmapImage(imageUri);
            t.Source = imageBitmap;
        }

        private void teamsCvs_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            var userTeam = (SC.API.ComInterop.Models.Team)e.Item;
            if (MatchOnSearchTeam(userTeam.Name))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private string searchStrTeam = "";
        private void tbSearchTeam_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tbSearchTeam.Text != "Search teams")
            {
                searchStrTeam = this.tbSearchTeam.Text.ToLower();
                this.teamsCvs.View.Refresh();
            }
        }

        private bool MatchOnSearchTeam(string str)
        {
            if (!string.IsNullOrEmpty(searchStrTeam))
            {
                str = str.ToLower();
                return str.Contains(searchStrTeam);
            }
            return true;
        }

        private void tbSearchTeam_GotFocus(object sender, RoutedEventArgs e)
        {
            this.tbSearchTeam.Text = "";
            this.tbSearchTeam.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
        }

        private void tbSearchTeam_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.tbSearchTeam.Text == "")
            {
                this.tbSearchTeam.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(194, 194, 194));
                this.tbSearchTeam.Text = "Search teams";
            }
        }
        private void storiesCvs_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            var userStory = (SC.API.ComInterop.Models.StoryLite)e.Item;
            if (MatchOnSearch(userStory.Name))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void tbSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            this.tbSearch.Text = "";
            this.tbSearch.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
        }

        private void tbSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.tbSearch.Text == "")
            {
                this.tbSearch.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(194, 194, 194));
                this.tbSearch.Text = "Search stories";
            }
        }

        private void AddStoryLite(StoryLite storyLite)
        {
            SelectedStoryLites.Add(storyLite); 
        }

        private void RemoveStoryLite(StoryLite storyLite)
        {
            SelectedStoryLites.Remove(SelectedStoryLites.Find(s => s.Id == storyLite.Id));
        }


        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb != null)
            {
                var sl = cb.DataContext as StoryLite;
                if (sl != null)
                {
                    if (!_allowMultiSelect)
                        SelectedStoryLites.Clear();

                    AddStoryLite(sl);
                }
            }
        }

        private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb != null)
            {
                var sl = cb.DataContext as StoryLite;
                if (sl != null)
                {
                    RemoveStoryLite(sl);
                }
            }
        }

    }
}
