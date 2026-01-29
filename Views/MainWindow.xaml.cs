using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Sleipnir.App.ViewModels;
using Sleipnir.App.Services;

namespace Sleipnir.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var dataService = new MockDataService();
            var viewModel = new MainViewModel(dataService);
            DataContext = viewModel;

            Loaded += async (s, e) => await viewModel.LoadDataCommand.ExecuteAsync(null);
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

        private void SprintSelected_Click(object sender, RoutedEventArgs e)
        {
            // The command is handled in VM. We just close the popup.
            DependencyObject current = sender as DependencyObject;
            while (current != null && !(current is Popup))
                current = VisualTreeHelper.GetParent(current);
            
            if (current is Popup popup) popup.IsOpen = false;
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            var input = value.ToString();
            var target = parameter.ToString();
            return input.Equals(target, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Visible;
            var input = value.ToString();
            var targets = parameter.ToString().Split('|');
            foreach (var target in targets)
            {
                if (input.Equals(target.Trim(), StringComparison.OrdinalIgnoreCase)) return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? 1.0 : 0.4;
            return 1.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class EqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            string val = value.ToString() ?? "";
            string param = parameter.ToString() ?? "";
            bool isInverse = param.StartsWith("NOT:");
            if (isInverse) param = param.Substring(4);
            bool isEqual = val.Equals(param, StringComparison.OrdinalIgnoreCase);
            if (isInverse) isEqual = !isEqual;
            return isEqual ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            if (parameter?.ToString() == "Inverse") isNull = !isNull;
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double defaultValue = 0.0;
            if (parameter != null) double.TryParse(parameter.ToString(), out defaultValue);
            if (value is bool b) return b ? defaultValue : 0.0;
            return 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MultiplierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && parameter != null && double.TryParse(parameter.ToString(), out double multiplier))
                return d * multiplier;
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
