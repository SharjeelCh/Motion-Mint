using StretchTracker.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace StretchTracker.Core
{
    public class NotificationManager
    {
        private DispatcherTimer _notificationTimer;
        private readonly DatabaseManager _dbManager;
        private readonly AppSettings _settings;
        private Window _notificationWindow;
        private readonly bool _testMode = true;
        private readonly int _testIntervalMinutes = 5; // 2-minute test interval

        public NotificationManager(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _settings = AppSettings.Load();
        }

        public void StartNotificationTimer()
        {
            try
            {
                // Use DispatcherTimer for WPFUpdateTimerInterval
                _notificationTimer = new DispatcherTimer();

                // Set interval based on settings (including dev mode if enabled)
                UpdateTimerInterval();

                _notificationTimer.Tick += (s, e) => ShowNotification();
                _notificationTimer.Start();

                // Show first notification after a short delay (10 seconds)
                DispatcherTimer startupTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };

                startupTimer.Tick += (s, e) =>
                {
                    ShowNotification();
                    startupTimer.Stop(); // One-time timer
                };

                startupTimer.Start();

                // Log the configured interval
                Console.WriteLine($"Notification timer started. Interval: {GetIntervalDescription()}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting notification: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTimerInterval()
        {
            // Get effective interval (considering dev mode)
            int intervalMinutes = _settings.GetEffectiveIntervalMinutes();

            // Ensure at least 5-minute interval
            intervalMinutes = Math.Max(5, intervalMinutes);

            // Set the timer interval
            _notificationTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);

            Console.WriteLine($"Timer interval updated to {intervalMinutes} minutes");
        }

        private string GetIntervalDescription()
        {
            if (_settings.EnableDevMode)
            {
                return $"{_settings.DevModeIntervalMinutes} minutes (Dev Mode)";
            }

            // Format the normal interval description
            var parts = new System.Collections.Generic.List<string>();

            if (_settings.IntervalDays > 0)
            {
                parts.Add($"{_settings.IntervalDays} day{(_settings.IntervalDays > 1 ? "s" : "")}");
            }

            if (_settings.IntervalHours > 0)
            {
                parts.Add($"{_settings.IntervalHours} hour{(_settings.IntervalHours > 1 ? "s" : "")}");
            }

            if (_settings.IntervalMinutes > 0)
            {
                parts.Add($"{_settings.IntervalMinutes} minute{(_settings.IntervalMinutes > 1 ? "s" : "")}");
            }

            return string.Join(", ", parts);
        }

        public void ShowNotification()
        {
            try
            {
                // If previous notification is still open, close it
                if (_notificationWindow != null && _notificationWindow.IsVisible)
                {
                    _notificationWindow.Close();
                    _notificationWindow = null;
                }

                // Get current streak from database
                int currentStreak = _dbManager.GetCurrentStreak();

                // Create simple notification window
                _notificationWindow = new Window
                {
                    Title = "Time to Stretch!",
                    Width = 350,
                    Height = 200,
                    WindowStyle = WindowStyle.ToolWindow,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true,
                    ShowInTaskbar = true
                };

                // Create the content
                Grid mainGrid = new Grid();

                // Create a stack panel for the content
                StackPanel contentPanel = new StackPanel
                {
                    Margin = new Thickness(20)
                };

                // Title
                TextBlock titleText = new TextBlock
                {
                    Text = "Time to stretch!",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // Message
                TextBlock messageText = new TextBlock
                {
                    Text = $"Maintain your {currentStreak}-day streak by completing today's stretch.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                // Buttons
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Button skipButton = new Button
                {
                    Content = "Skip",
                    Padding = new Thickness(15, 5, 15, 5),
                    Margin = new Thickness(0, 0, 10, 0)
                };

                Button startButton = new Button
                {
                    Content = "Start Stretching",
                    Padding = new Thickness(15, 5, 15, 5),
                    Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    Foreground = new SolidColorBrush(Colors.White)
                };

                // Button event handlers
                skipButton.Click += (s, e) =>
                {
                    // Close window first, then process the action
                    Window windowToClose = _notificationWindow;
                    _notificationWindow = null;
                    windowToClose.Close();
                };

                startButton.Click += (s, e) =>
                {
                    try
                    {
                        // Close window first
                        Window windowToClose = _notificationWindow;
                        _notificationWindow = null;
                        windowToClose.Close();

                        // Then open stretch window AFTER notification is closed
                        var stretchWindow = new StretchWindow(_dbManager);
                        stretchWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening stretch window: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                // Assemble the UI
                buttonPanel.Children.Add(skipButton);
                buttonPanel.Children.Add(startButton);

                contentPanel.Children.Add(titleText);
                contentPanel.Children.Add(messageText);
                contentPanel.Children.Add(buttonPanel);

                mainGrid.Children.Add(contentPanel);
                _notificationWindow.Content = mainGrid;

                // Play a sound if enabled
                if (_settings.PlaySoundOnCompletion)
                {
                    try
                    {
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                    catch
                    {
                        // Ignore sound errors - not critical
                    }
                }

                // Show the window
                _notificationWindow.Show();

                // Log next reminder time
                DateTime nextReminderTime = DateTime.Now.AddMinutes(_settings.GetEffectiveIntervalMinutes());
                Console.WriteLine($"Notification shown. Next reminder at: {nextReminderTime:g}");

                // Auto-close after 60 seconds
                DispatcherTimer closeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(60)
                };

                closeTimer.Tick += (s, e) =>
                {
                    if (_notificationWindow != null && _notificationWindow.IsVisible)
                    {
                        _notificationWindow.Close();
                        _notificationWindow = null;
                    }
                    closeTimer.Stop();
                };

                closeTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing notification: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateNotificationSchedule()
        {
            try
            {
                if (_notificationTimer != null)
                {
                    // Reload settings in case they've changed
                    AppSettings updatedSettings = AppSettings.Load();
                    _settings.IntervalDays = updatedSettings.IntervalDays;
                    _settings.IntervalHours = updatedSettings.IntervalHours;
                    _settings.IntervalMinutes = updatedSettings.IntervalMinutes;
                    _settings.EnableDevMode = updatedSettings.EnableDevMode;
                    _settings.DevModeIntervalMinutes = updatedSettings.DevModeIntervalMinutes;

                    // Update total minutes
                    _settings.UpdateTotalMinutes();

                    // Update the timer interval
                    _notificationTimer.Stop();
                    UpdateTimerInterval();
                    _notificationTimer.Start();

                    Console.WriteLine($"Notification schedule updated: {GetIntervalDescription()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating schedule: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void UpdateNotificationSchedule(int intervalMinutes)
        {
            try
            {
                if (_notificationTimer != null)
                {
                    // Don't override test mode timer
                    if (!_testMode)
                    {
                        _notificationTimer.Stop();
                        _notificationTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                        _notificationTimer.Start();

                        _settings.NotificationIntervalMinutes = intervalMinutes;
                        _settings.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating schedule: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CleanUp()
        {
            try
            {
                // Stop timer
                if (_notificationTimer != null)
                {
                    _notificationTimer.Stop();
                    _notificationTimer = null;
                }

                // Close any open notification windows
                if (_notificationWindow != null && _notificationWindow.IsVisible)
                {
                    _notificationWindow.Close();
                    _notificationWindow = null;
                }
            }
            catch (Exception ex)
            {
                // Just log, don't show message during shutdown
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}