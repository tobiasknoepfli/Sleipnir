using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;
using Sleipnir.App.ViewModels;

namespace Sleipnir.App.Utils
{
    public class AssigneeToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string responsibleUsers || values[1] is not MainViewModel viewModel)
                return null;

            if (string.IsNullOrWhiteSpace(responsibleUsers))
                return null;

            var userNames = responsibleUsers.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var userName in userNames)
            {
                var collab = viewModel.Collaborators.FirstOrDefault(c => c.Name == userName);
                if (collab != null)
                {
                    if (Enum.TryParse<MahApps.Metro.IconPacks.PackIconMaterialKind>(collab.Emoji, out var iconKind))
                    {
                        var icon = new MahApps.Metro.IconPacks.PackIconMaterial
                        {
                            Kind = iconKind,
                            Width = 16,
                            Height = 16,
                            Margin = new Thickness(0, 0, 5, 0),
                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                            ToolTip = collab.Name
                        };
                        panel.Children.Add(icon);
                    }
                }
            }

            return panel;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
