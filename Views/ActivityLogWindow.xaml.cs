using System.Collections.Generic;
using System.Windows;
using Sleipnir.App.Models;

namespace Sleipnir.App.Views
{
    public partial class ActivityLogWindow : Window
    {
        public ActivityLogWindow(List<IssueLog> logs)
        {
            InitializeComponent();
            LogsList.ItemsSource = logs;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public static void Show(Window owner, List<IssueLog> logs)
        {
            var win = new ActivityLogWindow(logs)
            {
                Owner = owner
            };
            win.ShowDialog();
        }
    }
}
