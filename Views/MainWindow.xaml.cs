using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using System.Linq;
using Sleipnir.App.Models;
using Sleipnir.App.ViewModels;
using Sleipnir.App.Services;

namespace Sleipnir.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (s, e) => 
            {
                await viewModel.LoadDataCommand.ExecuteAsync(null);
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void AllIssues_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedSprint = null;
            }
        }

        private Point _dragStartPoint;

        private void IssueCard_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void IssueCard_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is FrameworkElement fe && fe.DataContext is Issue issue)
                    {
                        DragDrop.DoDragDrop(fe, issue, DragDropEffects.Move);
                    }
                }
            }
        }

        private async void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Issue)) is Issue issue)
            {
                if (issue.Status == "Archived" && DataContext is MainViewModel vm)
                {
                    // Find the TabItem to get the Tag
                    DependencyObject? current = sender as DependencyObject;
                    while (current != null && !(current is TabItem))
                        current = VisualTreeHelper.GetParent(current);

                    if (current is TabItem tabItem && tabItem.Tag is string targetCategory)
                    {
                        // Ensure we are dropping into the right category
                        if (issue.Category.Equals(targetCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            await vm.RestoreIssueCommand.ExecuteAsync(issue);
                        }
                    }
                }
            }
        }
        private void Column_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Issue)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private async void Column_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Issue)) is Issue issue && DataContext is MainViewModel vm)
            {
                if (sender is FrameworkElement fe && fe.Tag is string targetStatus)
                {
                    await vm.ChangeIssueStatusAsync(issue, targetStatus);
                }
            }
        }

        private void SprintSelected_Click(object sender, RoutedEventArgs e)
        {
            // The command is handled in VM. We just close the popup.
            DependencyObject? current = sender as DependencyObject;
            while (current != null && !(current is Popup))
                current = VisualTreeHelper.GetParent(current);
            
            if (current is Popup popup) popup.IsOpen = false;
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is AppUser user)
            {
                user.IsPasswordRevealed = !user.IsPasswordRevealed;
            }
        }
        private async void IssueTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is Issue issue && DataContext is MainViewModel vm)
            {
                await vm.UpdateIssueAsync(issue);
            }
        }

        private void IssueTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
            {
                // Remove focus to trigger LostFocus and thus save
                Keyboard.ClearFocus();
                // Alternatively, find a focusable parent to move focus to
                this.Focus();
                e.Handled = true;
            }
        }

        private void PeekButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                // Find the CollapsibleDetails panel in the same card
                var border = FindVisualParent<Border>(btn);
                if (border != null)
                {
                    var collapsibleDetails = FindVisualChild<StackPanel>(border, "CollapsibleDetails");
                    if (collapsibleDetails != null)
                    {
                        collapsibleDetails.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void PeekButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                // Find the CollapsibleDetails panel and hide it again
                var border = FindVisualParent<Border>(btn);
                if (border != null)
                {
                    var collapsibleDetails = FindVisualChild<StackPanel>(border, "CollapsibleDetails");
                    if (collapsibleDetails != null && DataContext is MainViewModel vm && vm.AreCardsCollapsed)
                    {
                        collapsibleDetails.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is T tParent) return tParent;
            return FindVisualParent<T>(parent);
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild && tChild.Name == name)
                    return tChild;
                
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
