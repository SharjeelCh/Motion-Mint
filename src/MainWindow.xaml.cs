using StretchTracker.UI;
using System.Windows;

namespace StretchTracker
{
    public partial class MainWindow : Window
    {
        private readonly string[] _tipMessages = new string[]
        {
            "Aim for at least 5-10 minutes of stretching at a time. Focus on major muscle groups and hold each stretch for 15-30 seconds.",
            "Remember to breathe deeply while stretching. Exhale as you stretch, and inhale as you return to the starting position.",
            "Never bounce when stretching. Hold each stretch steady and avoid jerky movements to prevent injury.",
            "Stretching works best when your muscles are warm. Try a 5-minute light warm-up before stretching.",
            "Consistency is key! Regular stretching is more effective than occasional intense sessions."
        };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            // Add handler for the closing event
            Closing += MainWindow_Closing;

            // Set a random tip text
            Random random = new Random();
            TipText.Text = _tipMessages[random.Next(_tipMessages.Length)];
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get app instance to access the database manager
                var app = (App)Application.Current;

                // Update streak count
                int currentStreak = app.DatabaseManager.GetCurrentStreak();
                StreakCount.Text = currentStreak.ToString();

                // Update streak message
                if (currentStreak == 0)
                {
                    StreakMessage.Text = "Start your streak today!";
                }
                else if (currentStreak < 3)
                {
                    StreakMessage.Text = "Good start! Keep going!";
                }
                else if (currentStreak < 7)
                {
                    StreakMessage.Text = "Great progress! Keep it up!";
                }
                else if (currentStreak < 14)
                {
                    StreakMessage.Text = "Excellent work! You're building a habit!";
                }
                else
                {
                    StreakMessage.Text = "Amazing dedication! You're a stretching pro!";
                }
            }
            catch (Exception ex)
            {
                // Log error but don't disrupt the UI
                Console.WriteLine($"Error loading streak data: {ex.Message}");
            }
        }
        // Handle the window closing event to minimize to tray instead
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If we're not shutting down the application
            if (Application.Current.ShutdownMode != ShutdownMode.OnExplicitShutdown)
            {
                e.Cancel = true; // Prevent the window from closing
                this.Hide(); // Hide the window instead

                // Optional: Show a tooltip notification to let the user know the app is still running
                try
                {
                    var notifyIcon = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)Application.Current.Resources["NotifyIcon"];
                    if (notifyIcon != null)
                    {
                        notifyIcon.ShowBalloonTip(
                            "Stretch Tracker",
                            "The app is still running in the system tray. You'll receive Stretch Trackers as scheduled.",
                            Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error showing balloon tip: {ex.Message}");
                }
            }
        }
        private void StretchNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get app instance to access the database manager
                var app = (App)Application.Current;

                // Open the stretch window
                var stretchWindow = new StretchWindow(app.DatabaseManager);
                stretchWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting stretch session: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewStatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get app instance to access the database manager
                var app = (App)Application.Current;

                // Open the stats window
                var statsWindow = new StatsWindow(app.DatabaseManager);
                statsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening stats: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the settings window
                var settingsWindow = new SettingsWindow();
                settingsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}