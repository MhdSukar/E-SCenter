using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Services;
using ESCenter.Windows;

namespace ESCenter.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private bool _launchAtWindowsStartup;
        public bool LaunchAtWindowsStartup
        {
            get => _launchAtWindowsStartup;
            set
            {
                if (SetProperty(ref _launchAtWindowsStartup, value))
                {
                    UserPreferencesService.SetLaunchAtWindowsStartup(value);
                    if (!WindowsStartupService.SetLaunchAtStartup(value))
                    {
                        _launchAtWindowsStartup = WindowsStartupService.IsLaunchAtStartupEnabled();
                        OnPropertyChanged(nameof(LaunchAtWindowsStartup));
                        AppLogger.Warning("Unable to update Windows startup setting.");
                    }
                }
            }
        }

        private bool _autoGrantAdminAccess;
        public bool AutoGrantAdminAccess
        {
            get => _autoGrantAdminAccess;
            set
            {
                if (SetProperty(ref _autoGrantAdminAccess, value))
                {
                    UserPreferencesService.SetAutoGrantAdminAccess(value);
                }
            }
        }

        private bool _autoBackupEnabled;
        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set
            {
                if (SetProperty(ref _autoBackupEnabled, value))
                {
                    UserPreferencesService.SetAutoBackupEnabled(value);
                }
            }
        }

        private int _backupIntervalHours;
        public int BackupIntervalHours
        {
            get => _backupIntervalHours;
            set
            {
                var clamped = Math.Max(1, Math.Min(168, value));
                if (SetProperty(ref _backupIntervalHours, clamped))
                {
                    UserPreferencesService.SetBackupIntervalHours(clamped);
                    // Update running service interval if needed
                    var svc = AppServices.Get<BackupService>();
                    svc.UpdateInterval(TimeSpan.FromHours(clamped));
                }
            }
        }

        private string _backupLocation = string.Empty;
        public string BackupLocation
        {
            get => _backupLocation;
            set
            {
                if (SetProperty(ref _backupLocation, value))
                {
                    UserPreferencesService.SetBackupLocation(value);
                }
            }
        }

        private int _backupRetentionCount;
        public int BackupRetentionCount
        {
            get => _backupRetentionCount;
            set
            {
                var clamped = Math.Max(0, Math.Min(999, value));
                if (SetProperty(ref _backupRetentionCount, clamped))
                {
                    UserPreferencesService.SetBackupRetentionCount(clamped);
                }
            }
        }

        private string _pickupMessageLanguage = "English";
        public string PickupMessageLanguage
        {
            get => _pickupMessageLanguage;
            set
            {
                var normalized = string.Equals(value?.Trim(), "Arabic", StringComparison.OrdinalIgnoreCase)
                    ? "Arabic"
                    : "English";

                if (SetProperty(ref _pickupMessageLanguage, normalized))
                {
                    UserPreferencesService.SetPickupMessageLanguage(normalized);
                }
            }
        }

        public string[] PickupMessageLanguages { get; } = { "English", "Arabic" };

        private string _dashboardFinalCostCurrency = "S.P";
        public string DashboardFinalCostCurrency
        {
            get => _dashboardFinalCostCurrency;
            set
            {
                var normalized = string.Equals(value?.Trim(), "USD", StringComparison.OrdinalIgnoreCase)
                    ? "USD"
                    : "S.P";

                if (SetProperty(ref _dashboardFinalCostCurrency, normalized))
                {
                    UserPreferencesService.SetDashboardFinalCostCurrency(normalized);
                }
            }
        }

        public string[] DashboardCostCurrencies { get; } = { "S.P", "USD" };



        private int _autoRefreshIntervalSeconds;
        public int AutoRefreshIntervalSeconds
        {
            get => _autoRefreshIntervalSeconds;
            set
            {
                var normalized = Math.Max(0, value);
                if (SetProperty(ref _autoRefreshIntervalSeconds, normalized))
                {
                    UserPreferencesService.SetAutoRefreshIntervalSeconds(normalized);
                    if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel)
                    {
                        mainViewModel.ApplyAutoRefreshInterval(normalized);
                    }
                }
            }
        }

        private int _partsLowStockThreshold;
        public int PartsLowStockThreshold
        {
            get => _partsLowStockThreshold;
            set
            {
                var clamped = Math.Max(1, value);
                if (SetProperty(ref _partsLowStockThreshold, clamped))
                {
                    UserPreferencesService.SetLowStockThresholds(PartsLowStockThreshold, InventoryLowStockThreshold);
                }
            }
        }

        private int _inventoryLowStockThreshold;
        public int InventoryLowStockThreshold
        {
            get => _inventoryLowStockThreshold;
            set
            {
                var clamped = Math.Max(1, value);
                if (SetProperty(ref _inventoryLowStockThreshold, clamped))
                {
                    UserPreferencesService.SetLowStockThresholds(PartsLowStockThreshold, InventoryLowStockThreshold);
                }
            }
        }

        public string CurrentDatabasePath => DatabasePathService.CurrentDatabasePath;

        private string _integrityCheckResult = string.Empty;
        public string IntegrityCheckResult
        {
            get => _integrityCheckResult;
            set => SetProperty(ref _integrityCheckResult, value);
        }

        public string AppVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public ICommand ResetSettingsCommand { get; }
        public ICommand LoadDatabaseCommand { get; }
        public ICommand InitializeDatabaseCommand { get; }
        public ICommand ResetDatabaseCommand { get; }
        public ICommand CheckDatabaseIntegrityCommand { get; }
        public ICommand ChangeAdminPasswordCommand { get; }
        public ICommand ExportSettingsCommand { get; }
        public ICommand ImportSettingsCommand { get; }
        public ICommand BackupNowCommand { get; }
        public ICommand OpenLogFolderCommand { get; }

        public SettingsViewModel()
        {
            _launchAtWindowsStartup = UserPreferencesService.GetLaunchAtWindowsStartup();
            _autoGrantAdminAccess = UserPreferencesService.GetAutoGrantAdminAccess();
            _autoBackupEnabled = UserPreferencesService.GetAutoBackupEnabled();
            _backupIntervalHours = UserPreferencesService.GetBackupIntervalHours();
            _backupLocation = UserPreferencesService.GetBackupLocation();
            _backupRetentionCount = UserPreferencesService.GetBackupRetentionCount();
            _pickupMessageLanguage = UserPreferencesService.GetPickupMessageLanguage();
            _dashboardFinalCostCurrency = UserPreferencesService.GetDashboardFinalCostCurrency();
            _autoRefreshIntervalSeconds = UserPreferencesService.GetAutoRefreshIntervalSeconds();
            var lowStockThresholds = UserPreferencesService.GetLowStockThresholds();
            _partsLowStockThreshold = Math.Max(1, lowStockThresholds.PartsThreshold);
            _inventoryLowStockThreshold = Math.Max(1, lowStockThresholds.InventoryThreshold);

            ResetSettingsCommand = new RelayCommand(_ => ResetSettings());
            LoadDatabaseCommand = new RelayCommand(_ => LoadDatabase());
            InitializeDatabaseCommand = new RelayCommand(_ => InitializeDatabase());
            ResetDatabaseCommand = new RelayCommand(_ => ResetDatabase());
            CheckDatabaseIntegrityCommand = new RelayCommand(_ => CheckDatabaseIntegrity());
            ChangeAdminPasswordCommand = new RelayCommand(_ => ChangeAdminPassword());
            ExportSettingsCommand = new RelayCommand(_ => ExportSettings());
            ImportSettingsCommand = new RelayCommand(_ => ImportSettings());
            BackupNowCommand = new RelayCommand(_ => BackupNow());
            OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());

            DatabasePathService.DatabasePathChanged += (_, _) => OnPropertyChanged(nameof(CurrentDatabasePath));
        }

        private void ResetSettings()
        {
            try
            {
                var confirm = MessageBox.Show(
                    "This will reset all app settings to default, including admin login and database selection. Continue?",
                    "Reset Settings",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                DatabasePathService.ResetToDefaultPath();
                UserPreferencesService.ResetToDefaultPreferences(DatabasePathService.DefaultPath);
                LaunchAtWindowsStartup = false;
                AutoGrantAdminAccess = false;
                BackupLocation = string.Empty;
                PickupMessageLanguage = "English";
                DashboardFinalCostCurrency = "S.P";

                MessageBox.Show("All app settings were reset to default.\nAdmin credentials are now default: Admin / Admin.", "Reset Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                AppLogger.Success("All app settings were reset to default.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to reset settings: {ex.Message}");
                MessageBox.Show($"Failed to reset settings:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDatabase()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Database",
                    Filter = "SQLite Database (*.sql;*.db;*.sqlite)|*.sql;*.db;*.sqlite|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                DatabasePathService.SetDatabasePath(dialog.FileName);

                var initializer = new DatabaseInitializer(dialog.FileName);
                initializer.EnsureDatabaseReady();

                MessageBox.Show($"Database loaded successfully and saved as default:\n{dialog.FileName}", "Load Database", MessageBoxButton.OK, MessageBoxImage.Information);
                AppLogger.Success($"Database switched to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load database: {ex.Message}");
                MessageBox.Show($"Failed to load database:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                var initializer = new DatabaseInitializer(DatabasePathService.CurrentDatabasePath);
                var report = initializer.EnsureDatabaseReady();

                MessageBox.Show($"Database structure check completed for:\n{DatabasePathService.CurrentDatabasePath}\n\n{report}", "Initialize Database", MessageBoxButton.OK, MessageBoxImage.Information);
                AppLogger.Success("Database structure validated.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Database initialization failed: {ex.Message}");
                MessageBox.Show($"Database initialization failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDatabase()
        {
            try
            {
                var confirm = MessageBox.Show($"This will permanently delete the current database file:\n{DatabasePathService.CurrentDatabasePath}\n\nContinue?", "Reset Database", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                var databasePath = DatabasePathService.CurrentDatabasePath;
                if (File.Exists(databasePath)) File.Delete(databasePath);

                var initializer = new DatabaseInitializer(databasePath);
                initializer.EnsureDatabaseReady();

                MessageBox.Show("Old database was deleted and a new empty database was created.", "Database Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                AppLogger.Success("Database was recreated successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to reset database: {ex.Message}");
                MessageBox.Show($"Failed to reset database:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckDatabaseIntegrity()
        {
            try
            {
                var dbPath = DatabasePathService.CurrentDatabasePath;
                if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                {
                    MessageBox.Show("No database file found.", "Integrity Check", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                using var reader = cmd.ExecuteReader();
                var result = string.Empty;
                while (reader.Read())
                {
                    result += reader.GetString(0) + "\n";
                }

                IntegrityCheckResult = result.Trim();
                MessageBox.Show(result, "Integrity Check Result", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Integrity check failed: {ex.Message}");
                MessageBox.Show($"Integrity check failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeAdminPassword()
        {
            try
            {
                var dialog = new ChangeAdminPasswordWindow { Owner = System.Windows.Application.Current.MainWindow };
                if (dialog.ShowDialog() == true && dialog.PasswordChanged)
                {
                    AppLogger.Success("Admin credentials changed successfully.");
                    MessageBox.Show("Admin credentials changed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to change admin credentials: {ex.Message}");
                MessageBox.Show($"Failed to change admin credentials:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSettings()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Settings",
                    Filter = "JSON Settings (*.json)|*.json",
                    FileName = "user-preferences.json"
                };

                if (dialog.ShowDialog() != true) return;

                var prefsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ESCenter", "user-preferences.json");
                if (!File.Exists(prefsPath))
                {
                    MessageBox.Show("No settings file to export.", "Export Settings", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                File.Copy(prefsPath, dialog.FileName, overwrite: true);
                MessageBox.Show("Settings exported successfully.", "Export Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to export settings: {ex.Message}");
                MessageBox.Show($"Failed to export settings:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSettings()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import Settings",
                    Filter = "JSON Settings (*.json)|*.json",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() != true) return;

                var json = File.ReadAllText(dialog.FileName);
                var prefs = JsonSerializer.Deserialize<UserPreferencesService.UserPreferences>(json);
                if (prefs == null)
                {
                    MessageBox.Show("Invalid settings file.", "Import Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ESCenter");
                Directory.CreateDirectory(destDir);
                var dest = Path.Combine(destDir, "user-preferences.json");
                File.Copy(dialog.FileName, dest, overwrite: true);
                UserPreferencesService.InvalidateCache();

                // reload
                LaunchAtWindowsStartup = UserPreferencesService.GetLaunchAtWindowsStartup();
                AutoGrantAdminAccess = UserPreferencesService.GetAutoGrantAdminAccess();
                AutoBackupEnabled = UserPreferencesService.GetAutoBackupEnabled();
                BackupIntervalHours = UserPreferencesService.GetBackupIntervalHours();
                BackupLocation = UserPreferencesService.GetBackupLocation();
                BackupRetentionCount = UserPreferencesService.GetBackupRetentionCount();
                PickupMessageLanguage = UserPreferencesService.GetPickupMessageLanguage();
                AutoRefreshIntervalSeconds = UserPreferencesService.GetAutoRefreshIntervalSeconds();

                MessageBox.Show("Settings imported successfully.", "Import Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to import settings: {ex.Message}");
                MessageBox.Show($"Failed to import settings:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupNow()
        {
            try
            {
                var svc = AppServices.Get<BackupService>();
                var ok = svc.TryCreateBackupNow();
                if (ok)
                {
                    AppLogger.Success("Manual backup created.");
                    MessageBox.Show("Backup completed.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AppLogger.Warning("Manual backup failed or skipped.");
                    MessageBox.Show("Backup failed or skipped.", "Backup", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Manual backup failed: {ex.Message}");
                MessageBox.Show($"Backup failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenLogFolder()
        {
            try
            {
                var logDir = Core.FileLogger.LogDirectory;
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                {
                    AppLogger.Warning("Log folder not found or logging is disabled.");
                    return;
                }

                var todayLog = Core.FileLogger.TodayLogPath;
                if (todayLog != null && File.Exists(todayLog))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{todayLog}\"");
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", logDir);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open log folder: {ex.Message}");
            }
        }
    }
}
