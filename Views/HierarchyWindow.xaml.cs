using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Sleipnir.App.Models;

namespace Sleipnir.App.Views
{
    public partial class HierarchyWindow : Window
    {
        private readonly List<Issue> _allIssues;
        private readonly Issue _selectedIssue;
        private const double HorizontalGap = 250;
        private const double VerticalGap = 150;
        private const double NodeWidth = 200;
        private const double NodeHeight = 80;

        public HierarchyWindow(Issue selectedIssue, List<Issue> allIssues)
        {
            InitializeComponent();
            _allIssues = allIssues;
            _selectedIssue = selectedIssue;
            
            Loaded += (s, e) => BuildGraph();
        }

        private void BuildGraph()
        {
            GraphCanvas.Children.Clear();
            
            // 1. Find the root
            Issue root = _selectedIssue;
            while (root.ParentIssueId != null)
            {
                var parent = _allIssues.FirstOrDefault(i => i.Id == root.ParentIssueId);
                if (parent == null) break;
                root = parent;
            }

            SubtitleText.Text = $" - {root.Description}";

            // 2. Build layers
            var layers = new List<List<Issue>>();
            var currentLayer = new List<Issue> { root };
            
            while (currentLayer.Any())
            {
                layers.Add(currentLayer);
                var nextLayer = new List<Issue>();
                foreach (var issue in currentLayer)
                {
                    var children = _allIssues.Where(i => i.ParentIssueId == issue.Id).ToList();
                    nextLayer.AddRange(children);
                }
                currentLayer = nextLayer;
            }

            // 3. Position nodes
            var nodePositions = new Dictionary<Guid, Point>();
            double canvasWidth = layers.Max(l => l.Count) * HorizontalGap;
            double startY = 100;

            for (int l = 0; l < layers.Count; l++)
            {
                var layer = layers[l];
                double layerWidth = layer.Count * HorizontalGap;
                double startX = (GraphCanvas.Width - layerWidth) / 2 + (HorizontalGap / 2);

                for (int i = 0; i < layer.Count; i++)
                {
                    var issue = layer[i];
                    var pos = new Point(startX + i * HorizontalGap, startY + l * VerticalGap);
                    nodePositions[issue.Id] = pos;
                    DrawNode(issue, pos);
                }
            }

            // 4. Draw connections
            foreach (var posPair in nodePositions)
            {
                var childId = posPair.Key;
                var childPos = posPair.Value;
                var issue = _allIssues.FirstOrDefault(i => i.Id == childId);
                
                if (issue?.ParentIssueId != null && nodePositions.TryGetValue(issue.ParentIssueId.Value, out var parentPos))
                {
                    DrawConnection(parentPos, childPos);
                }
            }

            // Center on selected issue or root
            if (nodePositions.TryGetValue(_selectedIssue.Id, out var selectedPos))
            {
                GraphScrollViewer.ScrollToHorizontalOffset(selectedPos.X - GraphScrollViewer.ActualWidth / 2);
                GraphScrollViewer.ScrollToVerticalOffset(selectedPos.Y - GraphScrollViewer.ActualHeight / 2);
            }
        }

        private void DrawNode(Issue issue, Point pos)
        {
            var border = new Border
            {
                Width = NodeWidth,
                Height = NodeHeight,
                Background = new SolidColorBrush(Color.FromRgb(26, 29, 46)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, Opacity = 0.3, ShadowDepth = 5 }
            };

            if (issue.Id == _selectedIssue.Id)
            {
                border.BorderBrush = (Brush)Application.Current.Resources["AccentBrush"];
                border.BorderThickness = new Thickness(2);
            }

            var stack = new StackPanel();
            
            var typePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            var badgeColor = issue.Type switch
            {
                "Epic" => Color.FromRgb(255, 215, 64),
                "Story" => Color.FromRgb(255, 82, 82),
                _ => Color.FromRgb(77, 124, 255)
            };

            typePanel.Children.Add(new Border 
            { 
                Background = new SolidColorBrush(badgeColor), 
                CornerRadius = new CornerRadius(3), 
                Padding = new Thickness(4, 1, 4, 1),
                Child = new TextBlock { Text = issue.Type.ToUpper(), FontSize = 8, FontWeight = FontWeights.Bold, Foreground = Brushes.Black }
            });

            stack.Children.Add(typePanel);
            stack.Children.Add(new TextBlock 
            { 
                Text = issue.Description, 
                Foreground = Brushes.White, 
                FontSize = 11, 
                FontWeight = FontWeights.SemiBold, 
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 40,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            border.Child = stack;
            
            Canvas.SetLeft(border, pos.X - NodeWidth / 2);
            Canvas.SetTop(border, pos.Y - NodeHeight / 2);
            GraphCanvas.Children.Add(border);
        }

        private void DrawConnection(Point start, Point end)
        {
            var line = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                StrokeThickness = 2,
                Data = new PathGeometry
                {
                    Figures = new PathFigureCollection
                    {
                        new PathFigure
                        {
                            StartPoint = new Point(start.X, start.Y + NodeHeight / 2),
                            Segments = new PathSegmentCollection
                            {
                                new BezierSegment(
                                    new Point(start.X, start.Y + NodeHeight / 2 + 50),
                                    new Point(end.X, end.Y - NodeHeight / 2 - 50),
                                    new Point(end.X, end.Y - NodeHeight / 2),
                                    true
                                )
                            }
                        }
                    }
                }
            };
            
            // Insert line at the beginning so it's drawn behind nodes
            GraphCanvas.Children.Insert(0, line);
        }

        private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double zoom = e.Delta > 0 ? 1.1 : 0.9;
                GraphScale.ScaleX *= zoom;
                GraphScale.ScaleY *= zoom;
                e.Handled = true;
            }
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            GraphScale.ScaleX = 1;
            GraphScale.ScaleY = 1;
            GraphTranslate.X = 0;
            GraphTranslate.Y = 0;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
