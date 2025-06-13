using StretchTracker.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StretchTracker.UI
{
    public partial class StatsWindow : Window
    {
        private readonly DatabaseManager _dbManager;

        public StatsWindow(DatabaseManager dbManager)
        {
            InitializeComponent();

            _dbManager = dbManager;

            Loaded += (s, e) => LoadStats();
        }

        private void LoadStats()
        {
            // Get stats from database
            var sessionHistory = _dbManager.GetSessionHistory(30);
            int currentStreak = _dbManager.GetCurrentStreak();
            int totalSessions = _dbManager.GetTotalCompletedSessions();

            // Calculate completion rate
            int completedSessions = sessionHistory.Count(s => s.Completed);
            int totalRecentSessions = sessionHistory.Count;
            int completionRate = totalRecentSessions > 0 ? (completedSessions * 100 / totalRecentSessions) : 0;

            // Update UI elements
            CurrentStreakText.Text = currentStreak.ToString();
            CompletionRateText.Text = $"{completionRate}%";
            TotalSessionsText.Text = totalSessions.ToString();

            // Create calendar view
            CreateCalendarView(sessionHistory);

            // Create progress chart
            CreateProgressChart(sessionHistory);
        }

        private void CreateCalendarView(List<StretchSession> sessionHistory)
        {
            CalendarPanel.Children.Clear();

            // Group sessions by date for quick lookup
            var sessionsByDate = sessionHistory
                .GroupBy(s => s.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Get dates for last 30 days
            var today = DateTime.Now.Date;
            var dates = Enumerable.Range(0, 30)
                .Select(days => today.AddDays(-days))
                .Reverse()  // Display oldest to newest (left to right)
                .ToList();

            // Create visual calendar
            foreach (var date in dates)
            {
                // Create border for the day
                var dayBorder = new Border
                {
                    Width = 48,
                    Height = 48,
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224))
                };

                // Set background color based on completion status
                if (sessionsByDate.ContainsKey(date) && sessionsByDate[date].Any(s => s.Completed))
                {
                    dayBorder.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green for completed
                }
                else if (sessionsByDate.ContainsKey(date))
                {
                    dayBorder.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red for skipped
                }
                else if (date < today)
                {
                    dayBorder.Background = new SolidColorBrush(Color.FromRgb(238, 238, 238)); // Light gray for missed
                }
                else
                {
                    dayBorder.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // Gray for future
                }

                // Special treatment for today
                if (date == today)
                {
                    dayBorder.BorderThickness = new Thickness(2);
                    dayBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }

                // Create stack panel for content
                var panel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Add date number
                panel.Children.Add(new TextBlock
                {
                    Text = date.Day.ToString(),
                    FontWeight = date == today ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = date == today ? 16 : 14,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                // Add day of week
                if (date.DayOfWeek == DayOfWeek.Monday || date == today)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = date.ToString("ddd").Substring(0, 1), // First letter of day name
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }

                dayBorder.Child = panel;

                // Add tooltip with session details
                if (sessionsByDate.ContainsKey(date))
                {
                    var sessions = sessionsByDate[date];
                    string tooltip = date.ToString("MMMM d, yyyy") + "\n";

                    if (sessions.Any(s => s.Completed))
                    {
                        var completed = sessions.First(s => s.Completed);
                        tooltip += $"✓ Completed ({completed.Duration} seconds)";
                    }
                    else
                    {
                        tooltip += "✗ Skipped";
                    }

                    dayBorder.ToolTip = tooltip;
                }
                else
                {
                    dayBorder.ToolTip = date.ToString("MMMM d, yyyy") + "\nNo session";
                }

                CalendarPanel.Children.Add(dayBorder);
            }
        }

        private void CreateProgressChart(List<StretchSession> sessionHistory)
        {
            ChartCanvas.Children.Clear();

            // If no sessions yet, show empty state
            if (!sessionHistory.Any())
            {
                var emptyText = new TextBlock
                {
                    Text = "Complete your first stretching session to see your progress!",
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117))
                };

                Canvas.SetLeft(emptyText, 40);
                Canvas.SetTop(emptyText, 100);
                ChartCanvas.Children.Add(emptyText);
                return;
            }

            // Add instruction
            var instructionText = new TextBlock
            {
                Text = "Weekly completion rate over the last 30 days",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117))
            };
            Canvas.SetLeft(instructionText, 20);
            Canvas.SetTop(instructionText, 20);
            ChartCanvas.Children.Add(instructionText);

            // Get today and past dates for grouping
            var today = DateTime.Now.Date;
            var historyStart = today.AddDays(-29);

            // Group by week (Sunday to Saturday)
            var weekGroups = new Dictionary<string, List<StretchSession>>();

            // Initialize dictionary with week labels
            for (int i = 0; i < 5; i++) // Show past 4 weeks plus current week
            {
                DateTime weekStart = today.AddDays(-(int)today.DayOfWeek).AddDays(-7 * i);
                DateTime weekEnd = weekStart.AddDays(6);
                string weekLabel = $"{weekStart:MMM dd} - {weekEnd:MMM dd}";
                weekGroups[weekLabel] = new List<StretchSession>();
            }

            // Group sessions by week
            foreach (var session in sessionHistory)
            {
                // Skip if outside our 30-day window
                if (session.Date < historyStart)
                    continue;

                // Find which week this belongs to
                DateTime sessionDate = session.Date.Date;
                DateTime weekStart = sessionDate.AddDays(-(int)sessionDate.DayOfWeek);
                DateTime weekEnd = weekStart.AddDays(6);
                string weekLabel = $"{weekStart:MMM dd} - {weekEnd:MMM dd}";

                // Add to that week's list if it exists in our dictionary
                if (weekGroups.ContainsKey(weekLabel))
                {
                    weekGroups[weekLabel].Add(session);
                }
            }

            // Calculate dimensions based on ChartGrid
            double canvasWidth = ChartGrid.ActualWidth;
            double canvasHeight = ChartGrid.ActualHeight;

            // Ensure minimum dimensions
            canvasWidth = Math.Max(canvasWidth, 800);
            canvasHeight = Math.Max(canvasHeight, 400);

            // Set Canvas size to match Grid
            ChartCanvas.Width = canvasWidth;
            ChartCanvas.Height = canvasHeight;

            // Chart settings
            double barMaxHeight = canvasHeight - 100; // More space for labels
            double barWidth = Math.Min(100, canvasWidth / (weekGroups.Count * 2)); // Wider bars
            double xSpacing = canvasWidth / (weekGroups.Count + 1);

            // Draw axis
            var axisLine = new Line
            {
                X1 = 60,
                Y1 = canvasHeight - 60,
                X2 = canvasWidth - 40,
                Y2 = canvasHeight - 60,
                Stroke = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                StrokeThickness = 2
            };
            ChartCanvas.Children.Add(axisLine);

            // Draw Y-axis label
            var yAxisLabel = new TextBlock
            {
                Text = "Completion %",
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                FontSize = 16
            };
            Canvas.SetLeft(yAxisLabel, 20);
            Canvas.SetTop(yAxisLabel, 20);
            ChartCanvas.Children.Add(yAxisLabel);

            // Draw bars for each week
            int index = 0;
            foreach (var week in weekGroups.OrderBy(kv => DateTime.Parse(kv.Key.Split('-')[0])))
            {
                index++;
                string weekLabel = week.Key;
                var sessions = week.Value;

                // Calculate completion percentage
                int totalSessions = Math.Max(7, sessions.Count); // Assume 7 days per week
                int completedSessions = sessions.Count(s => s.Completed);
                double completionPercentage = (double)completedSessions / totalSessions;

                // Draw the bar
                double xPos = xSpacing * index - barWidth / 2;
                double barHeight = barMaxHeight * completionPercentage;

                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(barHeight, 4), // Minimum visible height
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    RadiusX = 6,
                    RadiusY = 6
                };

                Canvas.SetLeft(bar, xPos);
                Canvas.SetTop(bar, canvasHeight - 60 - barHeight);
                ChartCanvas.Children.Add(bar);

                // Add fancy gradient overlay for visual appeal
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                gradient.GradientStops.Add(new GradientStop(Color.FromArgb(80, 255, 255, 255), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromArgb(10, 255, 255, 255), 1));

                var overlay = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(barHeight, 4),
                    Fill = gradient,
                    RadiusX = 6,
                    RadiusY = 6
                };

                Canvas.SetLeft(overlay, xPos);
                Canvas.SetTop(overlay, canvasHeight - 60 - barHeight);
                ChartCanvas.Children.Add(overlay);

                // Add percentage text
                var percentText = new TextBlock
                {
                    Text = $"{completionPercentage:P0}",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                };

                Canvas.SetLeft(percentText, xPos + barWidth / 2 - 20);
                Canvas.SetTop(percentText, canvasHeight - 60 - barHeight - 30);
                ChartCanvas.Children.Add(percentText);

                // Add x-axis label (week)
                var weekLabelText = new TextBlock
                {
                    Text = weekLabel,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Width = barWidth * 2,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117))
                };

                Canvas.SetLeft(weekLabelText, xPos + barWidth / 2 - weekLabelText.Width / 2);
                Canvas.SetTop(weekLabelText, canvasHeight - 50);
                ChartCanvas.Children.Add(weekLabelText);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}