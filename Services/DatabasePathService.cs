using System;
using System.IO;

namespace ESCenter.Services
{
    public static class DatabasePathService
    {
        private const string DefaultDatabaseFile = "E-SCenter.sql";

        private static readonly string DefaultDatabasePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultDatabaseFile);

        private static string _databasePath = ResolveInitialPath();

        public static event EventHandler? DatabasePathChanged;

        public static string CurrentDatabasePath => _databasePath;

        public static string DefaultPath => DefaultDatabasePath;

        private static string ResolveInitialPath()
        {
            var preferences = UserPreferencesService.Load();
            var preferredPath = preferences.PreferredDatabasePath?.Trim();

            if (!string.IsNullOrWhiteSpace(preferredPath) && File.Exists(preferredPath))
            {
                return preferredPath;
            }

            var lastCustomPath = preferences.LastCustomDatabasePath?.Trim();
            if (!string.IsNullOrWhiteSpace(lastCustomPath) && File.Exists(lastCustomPath))
            {
                return lastCustomPath;
            }

            return DefaultDatabasePath;
        }


        public static void ResetToDefaultPath()
        {
            _databasePath = DefaultDatabasePath;
            UserPreferencesService.ResetToDefaultPreferences(DefaultDatabasePath);
            DatabasePathChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SetDatabasePath(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Database path cannot be empty.", nameof(databasePath));
            }

            _databasePath = databasePath.Trim();
            UserPreferencesService.SetPreferredDatabasePath(_databasePath, DefaultDatabasePath);
            DatabasePathChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
