using Hardcodet.Wpf.TaskbarNotification;
using StretchTracker.Core;
using StretchTracker.UI;
using System.Drawing;
using System.Windows;

namespace StretchTracker
{
    public partial class App : Application
    {
        public NotificationManager NotificationManager { get; private set; }
        public DatabaseManager DatabaseManager { get; private set; }
        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // Initialize database
                DatabaseManager = new DatabaseManager();
                DatabaseManager.InitializeDatabase();

                // Setup notification system
                NotificationManager = new NotificationManager(DatabaseManager);

                // Get and initialize the taskbar icon from resources
                notifyIcon = (TaskbarIcon)Resources["NotifyIcon"];
                if (notifyIcon != null)
                {
                    // Set icon programmatically
                    try
                    {
                        // Try to use standard application icon
                        notifyIcon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }
                    catch
                    {
                        // Fallback to system information icon if app icon can't be loaded
                        notifyIcon.Icon = SystemIcons.Information;
                    }

                    // Set event handlers
                    notifyIcon.TrayBalloonTipClicked += (s, args) => ShowMainWindow();
                    notifyIcon.TrayMouseDoubleClick += (s, args) => ShowMainWindow();
                }

                // Create a main window instead of using system tray icon
                MainWindow = new MainWindow();
                MainWindow.Show();

                // Start notification timer
                NotificationManager.StartNotificationTimer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        // Common method to show or restore the main window
        private void ShowMainWindow()
        {
            if (MainWindow == null)
            {
                // Create a new main window if it doesn't exist
                MainWindow = new MainWindow();
            }

            // Show and activate the window
            if (!MainWindow.IsVisible)
                MainWindow.Show();

            if (MainWindow.WindowState == WindowState.Minimized)
                MainWindow.WindowState = WindowState.Normal;

            MainWindow.Activate();
            MainWindow.Focus();
        }

        // Menu click handlers for the taskbar context menu
        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Show the main window
            ShowMainWindow();
        }

        private void StatsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open stats window
            var statsWindow = new StatsWindow(DatabaseManager);
            statsWindow.Show();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open settings window
            var settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Exit the application
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Clean up notification system
                NotificationManager?.CleanUp();

                // Clean up the TaskbarIcon
                notifyIcon?.Dispose();
            }
            catch
            {
                // Ignore errors during shutdown
            }
            base.OnExit(e);
        }
    }
}