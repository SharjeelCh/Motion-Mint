// AppSettings.cs (Update to add days/hours/minutes)
using System.IO;
using System.Text.Json;

namespace StretchTracker.Core
{
    public class AppSettings
    {
        // Total reminder interval in minutes
        public int NotificationIntervalMinutes { get; set; } = 120; // Default: 2 hours

        // Components for configurable interval
        public int IntervalDays { get; set; } = 0;
        public int IntervalHours { get; set; } = 2;
        public int IntervalMinutes { get; set; } = 0;

        // Application settings
        public bool StartWithWindows { get; set; } = false;

        // Stretching session settings
        public int RequiredStretchCount { get; set; } = 5; // Default: 5 stretches required
        public int MinimumSessionDuration { get; set; } = 30; // Default: minimum 30 seconds

        // UI settings
        public bool ShowMotivationalMessages { get; set; } = true;
        public bool PlaySoundOnCompletion { get; set; } = true;

        // Model settings
        public float PoseDetectionThreshold { get; set; } = 0.7f;

        // Development mode settings
        public bool EnableDevMode { get; set; } = false;  // Enable shortened intervals for testing
        public int DevModeIntervalMinutes { get; set; } = 5; // 5-minute reminder in dev mode

        // Path to settings file
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StretchTracker",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        // Calculate NotificationIntervalMinutes from components if they exist
                        settings.UpdateTotalMinutes();
                        return settings;
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, return default settings
            }

            // Return default settings
            var defaultSettings = new AppSettings();
            defaultSettings.UpdateTotalMinutes();
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directoryPath) && directoryPath != null)
                    Directory.CreateDirectory(directoryPath);

                // Ensure components are in sync with total minutes
                UpdateComponentsFromTotal();

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception)
            {
                // Handle saving error if needed
            }
        }

        public void ResetToDefaults()
        {
            NotificationIntervalMinutes = 120;
            IntervalDays = 0;
            IntervalHours = 2;
            IntervalMinutes = 0;
            StartWithWindows = false;
            RequiredStretchCount = 5;
            MinimumSessionDuration = 30;
            ShowMotivationalMessages = true;
            PlaySoundOnCompletion = true;
            PoseDetectionThreshold = 0.7f;
            EnableDevMode = false;
            DevModeIntervalMinutes = 5;

            Save();
        }

        // Update the total minutes based on days, hours, minutes components
        public void UpdateTotalMinutes()
        {
            NotificationIntervalMinutes = (IntervalDays * 24 * 60) + (IntervalHours * 60) + IntervalMinutes;

            // Ensure a minimum interval of 1 minute
            if (NotificationIntervalMinutes < 5)
            {
                NotificationIntervalMinutes = 5;
                IntervalMinutes = 5;
            }
        }

        // Update the components based on the total minutes
        public void UpdateComponentsFromTotal()
        {
            int remainingMinutes = NotificationIntervalMinutes;

            // Calculate days
            IntervalDays = remainingMinutes / (24 * 60);
            remainingMinutes %= (24 * 60);

            // Calculate hours
            IntervalHours = remainingMinutes / 60;

            // Remaining minutes
            IntervalMinutes = remainingMinutes % 60;
        }

        // Get the effective interval (considering dev mode)
        public int GetEffectiveIntervalMinutes()
        {
            return EnableDevMode ? DevModeIntervalMinutes : NotificationIntervalMinutes;
        }
    }
}