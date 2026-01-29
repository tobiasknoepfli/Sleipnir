using System.Windows;
using System.Windows.Controls;
using Sleipnir.App.Models;
using Sleipnir.App.ViewModels;

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
            if (sender is Button btn && btn.DataContext is Issue idea)
            {
                _issue.ParentIssueId = idea.Id;
                _issue.ParentTitle = idea.Description;
                LinkIdeaToggleButton.IsChecked = false;
            }
        }

        private void Unlink_Click(object sender, RoutedEventArgs e)
        {
            _issue.ParentIssueId = null;
            _issue.ParentTitle = null;
        }
    }
}
