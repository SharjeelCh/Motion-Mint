using Microsoft.Win32;
using StretchTracker.Core;
using System.Windows;

namespace StretchTracker.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private bool _uiInitialized = false;

        public SettingsWindow()
        {
            InitializeComponent();

            // Load current settings
            _settings = AppSettings.Load();

            // Add a loaded event to ensure the UI is completely initialized
            this.Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize UI with current settings
                IntervalSlider.Value = _settings.NotificationIntervalMinutes;
                StartWithWindowsCheckbox.IsChecked = _settings.StartWithWindows;
                SoundNotificationCheckbox.IsChecked = _settings.PlaySoundOnCompletion;
                StretchCountSlider.Value = _settings.RequiredStretchCount;
                ThresholdSlider.Value = _settings.PoseDetectionThreshold;
                MotivationCheckbox.IsChecked = _settings.ShowMotivationalMessages;

                // Developer options
                if (DebugLoggingCheckbox != null) // Check if the control exists
                {
                    DebugLoggingCheckbox.IsChecked = _settings.EnableDevMode;
                }

                ModelPathTextBox.Text = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "stretch_detection_model.pb");

                _uiInitialized = true;

                // Update text displays after UI is initialized
                UpdateIntervalText();
                UpdateStretchCountText();
                UpdateThresholdText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIntervalText()
        {
            if (!_uiInitialized || IntervalTextBlock == null)
                return;

            int minutes = (int)IntervalSlider.Value;
            if (minutes >= 60)
            {
                int hours = minutes / 60;
                int remainingMinutes = minutes % 60;

                if (remainingMinutes == 0)
                    IntervalTextBlock.Text = $"Every {hours} hour{(hours > 1 ? "s" : "")}";
                else
                    IntervalTextBlock.Text = $"Every {hours} hour{(hours > 1 ? "s" : "")} and {remainingMinutes} minutes";
            }
            else
            {
                IntervalTextBlock.Text = $"Every {minutes} minutes";
            }
        }

        private void UpdateStretchCountText()
        {
            if (!_uiInitialized || StretchCountText == null)
                return;

            int count = (int)StretchCountSlider.Value;
            StretchCountText.Text = $"{count} stretches";
        }

        private void UpdateThresholdText()
        {
            if (!_uiInitialized || ThresholdText == null)
                return;

            double threshold = ThresholdSlider.Value;
            string description;

            if (threshold < 0.6)
                description = "Low (more detections)";
            else if (threshold < 0.75)
                description = "Medium";
            else
                description = "High (fewer detections)";

            ThresholdText.Text = description;
        }

        private void IntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateIntervalText();
        }

        private void StretchCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateStretchCountText();
        }

        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateThresholdText();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "TensorFlow Models (*.pb)|*.pb|All files (*.*)|*.*",
                InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models")
            };

            if (dialog.ShowDialog() == true)
            {
                ModelPathTextBox.Text = dialog.FileName;
            }
        }

        private void TestNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get app instance
                var app = (App)Application.Current;

                // Trigger a notification
                app.NotificationManager.ShowNotification();

                MessageBox.Show("Notification test sent successfully.",
                    "Test Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send test notification: {ex.Message}",
                    "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Confirm reset
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset settings
                _settings.ResetToDefaults();

                // Update UI
                IntervalSlider.Value = _settings.NotificationIntervalMinutes;
                StartWithWindowsCheckbox.IsChecked = _settings.StartWithWindows;
                SoundNotificationCheckbox.IsChecked = _settings.PlaySoundOnCompletion;
                StretchCountSlider.Value = _settings.RequiredStretchCount;
                ThresholdSlider.Value = _settings.PoseDetectionThreshold;
                MotivationCheckbox.IsChecked = _settings.ShowMotivationalMessages;
                //DebugLoggingCheckbox.IsChecked = _settings.EnableDebugLogging;

                UpdateIntervalText();
                UpdateStretchCountText();
                UpdateThresholdText();

                MessageBox.Show("Settings have been reset to defaults.",
                    "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update settings from UI
            _settings.NotificationIntervalMinutes = (int)IntervalSlider.Value;
            _settings.StartWithWindows = StartWithWindowsCheckbox.IsChecked ?? false;
            _settings.PlaySoundOnCompletion = SoundNotificationCheckbox.IsChecked ?? true;
            _settings.RequiredStretchCount = (int)StretchCountSlider.Value;
            _settings.PoseDetectionThreshold = (float)ThresholdSlider.Value;
            _settings.ShowMotivationalMessages = MotivationCheckbox.IsChecked ?? true;

            if (DebugLoggingCheckbox != null)
            {
                _settings.EnableDevMode = DebugLoggingCheckbox.IsChecked ?? false;
            }
            // Update components based on total minutes
            _settings.UpdateComponentsFromTotal();

            // Save settings to file
            _settings.Save();

            // Update notification interval in the app
            try
            {
                var app = (App)Application.Current;
                app.NotificationManager.UpdateNotificationSchedule();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating notification schedule: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Continue with the rest of the method even if this fails
            }

            // Update startup registry
            if (_settings.StartWithWindows)
                AddApplicationToStartup();
            else
                RemoveApplicationFromStartup();

            // Show confirmation and close the window
            MessageBox.Show("Settings saved successfully!",
                "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);

            // Close the window
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close without saving
            Close();
        }

        private void AddApplicationToStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        key.SetValue("StretchTracker",
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add to startup: {ex.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveApplicationFromStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null && key.GetValue("StretchTracker") != null)
                        key.DeleteValue("StretchTracker");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove from startup: {ex.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}