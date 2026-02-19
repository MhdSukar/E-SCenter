using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ESCenter.Services
{
    public static class UserPreferencesService
    {
        private const string SettingsFileName = "user-preferences.json";
        private const string LegacyDatabaseSettingsFileName = "database-settings.json";
        private const string DefaultAdminUsername = "Admin";
        private static readonly string DefaultAdminPasswordHash = ComputeHash("Admin");

        private static readonly string SettingsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ESCenter");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);
        private static readonly string LegacyDatabaseSettingsPath = Path.Combine(SettingsDirectory, LegacyDatabaseSettingsFileName);
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static UserPreferences Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    var migrated = TryLoadLegacyPreferences();
                    return Normalize(migrated ?? CreateDefaultPreferences());
                }

                var json = File.ReadAllText(SettingsPath);
                var preferences = JsonSerializer.Deserialize<UserPreferences>(json);
                return Normalize(preferences);
            }
            catch
            {
                return CreateDefaultPreferences();
            }
        }

        public static void Save(UserPreferences preferences)
        {
            var normalized = Normalize(preferences);

            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var json = JsonSerializer.Serialize(normalized, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }

        public static (int PartsThreshold, int InventoryThreshold) GetLowStockThresholds(int defaultValue = 3)
        {
            var preferences = Load();
            var parts = preferences.PartsLowStockThreshold < 0 ? defaultValue : preferences.PartsLowStockThreshold;
            var inventory = preferences.InventoryLowStockThreshold < 0 ? defaultValue : preferences.InventoryLowStockThreshold;
            return (parts, inventory);
        }

        public static void SetLowStockThresholds(int partsThreshold, int inventoryThreshold)
        {
            var preferences = Load();
            preferences.PartsLowStockThreshold = Math.Max(0, partsThreshold);
            preferences.InventoryLowStockThreshold = Math.Max(0, inventoryThreshold);
            Save(preferences);
        }

        public static void SetPreferredDatabasePath(string databasePath, string defaultDatabasePath)
        {
            var trimmedPath = databasePath?.Trim() ?? string.Empty;
            var preferences = Load();
            preferences.PreferredDatabasePath = string.IsNullOrWhiteSpace(trimmedPath)
                ? defaultDatabasePath
                : trimmedPath;

            if (!string.Equals(preferences.PreferredDatabasePath, defaultDatabasePath, StringComparison.OrdinalIgnoreCase))
            {
                preferences.LastCustomDatabasePath = preferences.PreferredDatabasePath;
            }

            Save(preferences);
        }

        public static void ResetToDefaultPreferences(string defaultDatabasePath)
        {
            var defaults = CreateDefaultPreferences();
            defaults.PreferredDatabasePath = defaultDatabasePath;
            Save(defaults);
        }

        public static bool ValidateAdminCredentials(string username, string password)
        {
            var preferences = Load();
            var expectedUser = string.IsNullOrWhiteSpace(preferences.AdminUsername)
                ? DefaultAdminUsername
                : preferences.AdminUsername.Trim();

            var expectedPasswordHash = string.IsNullOrWhiteSpace(preferences.AdminPasswordHash)
                ? DefaultAdminPasswordHash
                : preferences.AdminPasswordHash;

            return string.Equals(expectedUser, username?.Trim(), StringComparison.OrdinalIgnoreCase)
                   && string.Equals(expectedPasswordHash, ComputeHash(password ?? string.Empty), StringComparison.Ordinal);
        }

        public static bool ChangeAdminCredentials(string currentUsername, string currentPassword, string newUsername, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            if (!ValidateAdminCredentials(currentUsername, currentPassword))
            {
                return false;
            }

            var preferences = Load();
            preferences.AdminUsername = newUsername.Trim();
            preferences.AdminPasswordHash = ComputeHash(newPassword);
            Save(preferences);
            return true;
        }

        private static UserPreferences? TryLoadLegacyPreferences()
        {
            try
            {
                if (!File.Exists(LegacyDatabaseSettingsPath))
                {
                    return null;
                }

                var json = File.ReadAllText(LegacyDatabaseSettingsPath);
                var legacySettings = JsonSerializer.Deserialize<LegacyDatabaseSettings>(json);
                if (string.IsNullOrWhiteSpace(legacySettings?.DatabasePath))
                {
                    return null;
                }

                return new UserPreferences
                {
                    PreferredDatabasePath = legacySettings.DatabasePath,
                    LastCustomDatabasePath = legacySettings.DatabasePath,
                    PartsLowStockThreshold = 3,
                    InventoryLowStockThreshold = 3,
                    AlBarakaExecutablePath = string.Empty,
                    AdminUsername = DefaultAdminUsername,
                    AdminPasswordHash = DefaultAdminPasswordHash
                };
            }
            catch
            {
                return null;
            }
        }

        private static UserPreferences CreateDefaultPreferences() => new()
        {
            PreferredDatabasePath = string.Empty,
            LastCustomDatabasePath = string.Empty,
            PartsLowStockThreshold = 3,
            InventoryLowStockThreshold = 3,
            AlBarakaExecutablePath = string.Empty,
            AdminUsername = DefaultAdminUsername,
            AdminPasswordHash = DefaultAdminPasswordHash
        };

        private static UserPreferences Normalize(UserPreferences? preferences)
        {
            var normalized = preferences ?? CreateDefaultPreferences();

            if (string.IsNullOrWhiteSpace(normalized.AdminUsername))
            {
                normalized.AdminUsername = DefaultAdminUsername;
            }

            if (string.IsNullOrWhiteSpace(normalized.AdminPasswordHash))
            {
                normalized.AdminPasswordHash = DefaultAdminPasswordHash;
            }

            if (normalized.PartsLowStockThreshold < 0)
            {
                normalized.PartsLowStockThreshold = 0;
            }

            if (normalized.InventoryLowStockThreshold < 0)
            {
                normalized.InventoryLowStockThreshold = 0;
            }

            normalized.PreferredDatabasePath ??= string.Empty;
            normalized.LastCustomDatabasePath ??= string.Empty;
            normalized.AlBarakaExecutablePath ??= string.Empty;

            return normalized;
        }

        private static string ComputeHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        public sealed class UserPreferences
        {
            public string PreferredDatabasePath { get; set; } = string.Empty;
            public string LastCustomDatabasePath { get; set; } = string.Empty;
            public int PartsLowStockThreshold { get; set; } = 3;
            public int InventoryLowStockThreshold { get; set; } = 3;
            public string AlBarakaExecutablePath { get; set; } = string.Empty;
            public string AdminUsername { get; set; } = DefaultAdminUsername;
            public string AdminPasswordHash { get; set; } = DefaultAdminPasswordHash;
        }

        private sealed class LegacyDatabaseSettings
        {
            public string DatabasePath { get; set; } = string.Empty;
        }
    }
}
