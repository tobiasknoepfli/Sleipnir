using System.Windows;
using System.Windows.Controls;
using Sleipnir.App.Models;
using Sleipnir.App.ViewModels;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Linq;
using System.Collections.Generic;

namespace Sleipnir.App.Views
{
    public partial class IssueDetailWindow : Window
    {
        private readonly Issue _issue;
        private readonly MainViewModel _viewModel;

        public MainViewModel ViewModel => _viewModel;

        public IssueDetailWindow(Issue issue, MainViewModel viewModel)
        {
            InitializeComponent();
            _issue = issue;
            _viewModel = viewModel;
            DataContext = _issue;
            
            _viewModel.LoadPotentialParents(_issue);
            
            _issue.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Issue.ResponsibleUsers))
                {
                    UpdateAssigneeDisplay();
                }
            };

            Loaded += (s, e) => 
            {
                TitleTextBox.Focus();
                TitleTextBox.SelectAll();
                UpdateAssigneeDisplay();
            };
        }

        private void UpdateAssigneeDisplay()
        {
            if (AssigneeIconsPanel == null || _viewModel.Collaborators == null) return;

            var assignedUsers = string.IsNullOrWhiteSpace(_issue.ResponsibleUsers)
                ? new List<string>()
                : _issue.ResponsibleUsers.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            AssigneeIconsPanel.Children.Clear();

            if (!assignedUsers.Any())
            {
                var defaultIcon = new MahApps.Metro.IconPacks.PackIconMaterial
                {
                    Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Account,
                    Width = 18,
                    Height = 18,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 148, 158))
                };
                AssigneeIconsPanel.Children.Add(defaultIcon);
                AssigneesToggle.ToolTip = "Assign Responsibles...";
                return;
            }

            var names = new List<string>();

            foreach (var userName in assignedUsers)
            {
                var collab = _viewModel.Collaborators.FirstOrDefault(c => c.Name == userName);
                if (collab != null)
                {
                    // Try to parse the emoji string as a PackIconMaterialKind
                    if (Enum.TryParse<MahApps.Metro.IconPacks.PackIconMaterialKind>(collab.Emoji, out var iconKind))
                    {
                        var icon = new MahApps.Metro.IconPacks.PackIconMaterial
                        {
                            Kind = iconKind,
                            Width = 18,
                            Height = 18,
                            Margin = new Thickness(0, 0, 5, 0)
                        };
                        AssigneeIconsPanel.Children.Add(icon);
                        names.Add(collab.Name);
                    }
                }
            }

            AssigneesToggle.ToolTip = string.Join(", ", names);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            var logs = await _viewModel.DataService.GetLogsAsync(_issue.Id);
            ActivityLogWindow.Show(this, logs);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.UpdateIssueAsync(_issue);
            Close();
        }

        private void EpicSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Issue parent)
            {
                _issue.ParentIssueId = parent.Id;
                _issue.ParentTitle = parent.Description;
                _issue.ParentFriendlyId = parent.FriendlyId;
                
                if (FooterLinkToggle != null) FooterLinkToggle.IsChecked = false;
            }
        }

        private async void Unlink_Click(object sender, RoutedEventArgs e)
        {
            _issue.ParentIssueId = null;
            _issue.ParentTitle = null;
            _issue.ParentFriendlyId = null;
            _issue.ParentStoryFriendlyId = null;
            await _viewModel.UpdateIssueAsync(_issue);
        }

        private void EditChild_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Issue child)
            {
                _viewModel.OpenIssueDetailCommand.Execute(child);
            }
        }

        private async void UnlinkChild_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Issue child)
            {
                var result = ActionDialog.Show("Unlink", $"Are you sure you want to unlink '{child.Description}'?", _issue.Type, "Unlink");
                if (result == ActionDialog.DialogResultAction.Action1)
                {
                    await _viewModel.UnlinkIssueCommand.ExecuteAsync(child);
                }
            }
        }

        private async void DeleteChild_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Issue child)
            {
                var result = ActionDialog.Show("Delete", $"Are you sure you want to permanently delete '{child.Description}'?", _issue.Type, "Delete");
                if (result == ActionDialog.DialogResultAction.Action1)
                {
                    await _viewModel.DeleteIssueDirectAsync(child);
                }
            }
        }

        private async void Archive_Click(object sender, RoutedEventArgs e)
        {
            // Archive the epic/story - children remain untouched
            await _viewModel.ArchiveIssueCommand.ExecuteAsync(_issue);
            Close();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if ((_issue.Type == "Epic" || _issue.Type == "Story") && _issue.Children.Any())
            {
                // Automatically unlink all children
                foreach(var child in _issue.Children.ToList()) 
                    await _viewModel.UnlinkIssueCommand.ExecuteAsync(child);
                
                await _viewModel.DeleteIssueDirectAsync(_issue);
                Close();
            }
            else
            {
                var result = ActionDialog.Show("Delete", $"Are you sure you want to delete '{_issue.Description}'?", _issue.Type, "Delete");
                if (result == ActionDialog.DialogResultAction.Action1)
                {
                    await _viewModel.DeleteIssueDirectAsync(_issue);
                    Close();
                }
            }
        }

        private void AddAssignee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string username)
            {
                if (string.IsNullOrWhiteSpace(_issue.ResponsibleUsers))
                {
                    _issue.ResponsibleUsers = username;
                }
                else if (!_issue.ResponsibleUsers.Contains(username))
                {
                    _issue.ResponsibleUsers += "; " + username;
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is Collaborator collab)
            {
                var username = collab.Name;
                var users = string.IsNullOrWhiteSpace(_issue.ResponsibleUsers) 
                    ? new List<string>() 
                    : _issue.ResponsibleUsers.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (cb.IsChecked == true)
                {
                    if (!users.Contains(username)) users.Add(username);
                }
                else
                {
                    users.Remove(username);
                }

                _issue.ResponsibleUsers = string.Join("; ", users);
            }
        }

        private void SprintSelected_Click(object sender, RoutedEventArgs e)
        {
            // The command is handled in VM. We just close the popup.
            DependencyObject? current = sender as DependencyObject;
            while (current != null && !(current is Popup))
                current = VisualTreeHelper.GetParent(current);
            
            if (current is Popup popup) popup.IsOpen = false;
            if (DetailPlanToggle != null) DetailPlanToggle.IsChecked = false;
        }
        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scv)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }


}
