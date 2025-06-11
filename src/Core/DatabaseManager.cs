using Microsoft.Data.Sqlite;
using System.IO;

namespace StretchTracker.Core
{
    public class DatabaseManager
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseManager()
        {
            // Create database in AppData folder
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StretchTracker");

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            _dbPath = Path.Combine(appDataPath, "StretchData.db");
            _connectionString = $"Data Source={_dbPath}";
        }

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Create stretching sessions table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS StretchSessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Completed INTEGER NOT NULL,
                        Duration INTEGER NOT NULL
                    )";
                command.ExecuteNonQuery();
            }

            // Create settings table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
            }
        }

        public void RecordStretchSession(bool completed, int durationSeconds)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO StretchSessions (Date, Completed, Duration)
                VALUES (@date, @completed, @duration)";

            command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@completed", completed ? 1 : 0);
            command.Parameters.AddWithValue("@duration", durationSeconds);

            command.ExecuteNonQuery();
        }

        public int GetCurrentStreak()
        {
            int streak = 0;
            DateTime currentDate = DateTime.Now.Date;
            bool streakActive = true;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check if user completed today's session
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) FROM StretchSessions 
                    WHERE Date = @date AND Completed = 1";

                command.Parameters.AddWithValue("@date", currentDate.ToString("yyyy-MM-dd"));

                long todayCount = (long)command.ExecuteScalar();

                if (todayCount > 0)
                    streak = 1; // Start with 1 if today is completed
            }

            // Start checking from yesterday if today is already done,
            // otherwise start from today
            DateTime checkDate = streak > 0 ? currentDate.AddDays(-1) : currentDate;

            // Check previous days
            while (streakActive)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) FROM StretchSessions 
                    WHERE Date = @date AND Completed = 1";

                command.Parameters.AddWithValue("@date", checkDate.ToString("yyyy-MM-dd"));

                long count = (long)command.ExecuteScalar();

                if (count > 0)
                {
                    // If checking today and it's not completed yet, don't increment
                    if (!(checkDate == currentDate && streak == 0))
                        streak++;
                }
                else
                    streakActive = false;

                checkDate = checkDate.AddDays(-1);
            }

            return streak;
        }

        public List<StretchSession> GetSessionHistory(int days)
        {
            var sessions = new List<StretchSession>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Date, Completed, Duration FROM StretchSessions
                WHERE Date >= @startDate
                ORDER BY Date DESC";

            command.Parameters.AddWithValue("@startDate",
                DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new StretchSession
                {
                    Date = DateTime.Parse(reader.GetString(0)),
                    Completed = reader.GetInt32(1) == 1,
                    Duration = reader.GetInt32(2)
                });
            }

            return sessions;
        }

        public int GetTotalCompletedSessions()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM StretchSessions WHERE Completed = 1";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public class StretchSession
    {
        public DateTime Date { get; set; }
        public bool Completed { get; set; }
        public int Duration { get; set; }
    }
}