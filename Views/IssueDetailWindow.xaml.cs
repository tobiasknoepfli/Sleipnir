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

            Loaded += (s, e) => 
            {
                TitleTextBox.Focus();
                TitleTextBox.SelectAll();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.UpdateIssueAsync(_issue);
            Close();
        }

        private void IdeaSelected_Click(object sender, RoutedEventArgs e)
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
                var result = ActionDialog.Show("Unlink Story", $"Are you sure you want to unlink '{child.Description}' from this idea?", "Unlink");
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
                var result = ActionDialog.Show("Delete", $"Are you sure you want to PERMANENTLY delete '{child.Description}'?", "Delete");
                if (result == ActionDialog.DialogResultAction.Action1)
                {
                    await _viewModel.DeleteIssueDirectAsync(child);
                }
            }
        }

        private async void Archive_Click(object sender, RoutedEventArgs e)
        {
            if (_issue.Type == "Idea" && _issue.Children.Any())
            {
                var result = ActionDialog.Show("Archive Idea", 
                    "This idea has linked stories. What do you want to do with them?", 
                    "Archive Stories", "Unlink Stories");
                
                if (result == ActionDialog.DialogResultAction.Cancel) return;
                
                if (result == ActionDialog.DialogResultAction.Action1)
                {
                    foreach(var child in _issue.Children.ToList()) 
                        await _viewModel.ArchiveIssueCommand.ExecuteAsync(child);
                }
                else if (result == ActionDialog.DialogResultAction.Action2)
                {
                    foreach(var child in _issue.Children.ToList()) 
                        await _viewModel.UnlinkIssueCommand.ExecuteAsync(child);
                }
                
                await _viewModel.ArchiveIssueCommand.ExecuteAsync(_issue);
                Close();
            }
            else
            {
                await _viewModel.ArchiveIssueCommand.ExecuteAsync(_issue);
                Close();
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_issue.Type == "Idea" && _issue.Children.Any())
            {
                var result = ActionDialog.Show("Delete Idea", 
                    "This idea has linked stories. What do you want to do with them?", 
                    "Delete Stories", "Unlink Stories");
                
                if (result == ActionDialog.DialogResultAction.Cancel) return;

                if (result == ActionDialog.DialogResultAction.Action1)
                {
                    foreach(var child in _issue.Children.ToList()) 
                        await _viewModel.DeleteIssueDirectAsync(child);
                }
                else if (result == ActionDialog.DialogResultAction.Action2)
                {
                    foreach(var child in _issue.Children.ToList()) 
                        await _viewModel.UnlinkIssueCommand.ExecuteAsync(child);
                }
                
                await _viewModel.DeleteIssueDirectAsync(_issue);
                Close();
            }
            else
            {
                // For regular cases, if it's an Idea with no children, we still want a custom confirmation?
                // The user said: "if the custom popup appears, don't ask again".
                // If it's an Idea/Story with no linked items, the custom popup (action dialog) wouldn't appear currently.
                // Let's add the confirmation here too to be safe and consistent.
                
                var result = ActionDialog.Show("Delete", $"Are you sure you want to delete '{_issue.Description}'?", "Delete");
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
            if (sender is CheckBox cb && cb.Content is string username)
            {
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

    public class StringContainsMultiConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null) return false;
            string list = values[0].ToString() ?? "";
            string item = values[1].ToString() ?? "";
            return list.Split(';').Select(s => s.Trim()).Any(u => u.Equals(item, System.StringComparison.OrdinalIgnoreCase));
        }
        public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
