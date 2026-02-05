using System;
using System.IO;
using System.Text.Json;

namespace ESCenter.Services
{
    public static class DatabasePathService
    {
        private const string DefaultDatabaseFile = "E-SCenter.sql";
        private const string SettingsFileName = "database-settings.json";

        private static readonly string SettingsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ESCenter");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);

        private static string _databasePath = ResolveInitialPath();

        public static event EventHandler? DatabasePathChanged;

        public static string CurrentDatabasePath => _databasePath;

        private static string ResolveInitialPath()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<DatabaseSettings>(json);
                    if (!string.IsNullOrWhiteSpace(settings?.DatabasePath))
                    {
                        return settings.DatabasePath;
                    }
                }
            }
            catch
            {
                // fallback to default path
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultDatabaseFile);
        }

        public static void SetDatabasePath(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Database path cannot be empty.", nameof(databasePath));
            }

            _databasePath = databasePath.Trim();
            SaveSettings();
            DatabasePathChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void SaveSettings()
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var settings = new DatabaseSettings { DatabasePath = _databasePath };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        private sealed class DatabaseSettings
        {
            public string DatabasePath { get; set; } = string.Empty;
        }
    }
}
