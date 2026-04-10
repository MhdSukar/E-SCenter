using System;
using System.IO;
using ESCenter.Core;
using System.Windows.Threading;

namespace ESCenter.Services
{
    public sealed class BackupService : IDisposable
    {
        private const string BackupFolderName = "ESCenter Backups";
        private readonly DispatcherTimer _timer;

        public BackupService()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += OnBackupTick;

            // Initialize interval from preferences
            var hours = UserPreferencesService.GetBackupIntervalHours();
            _timer.Interval = TimeSpan.FromHours(hours <= 0 ? 6 : hours);
        }

        public void Start()
        {
            _timer.Start();
            AppLogger.Info($"Automatic backup service started. Interval: {_timer.Interval.TotalHours} hours.");
            TryCreateBackupNow();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void OnBackupTick(object? sender, EventArgs e)
        {
            try
            {
                if (!UserPreferencesService.GetAutoBackupEnabled())
                {
                    AppLogger.Info("Automatic backup skipped because AutoBackupEnabled is false.");
                    return;
                }

                TryCreateBackupNow();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Automatic backup tick failed: {ex.Message}");
            }
        }

        public bool TryCreateBackupNow()
        {
            try
            {
                var databasePath = DatabasePathService.CurrentDatabasePath;
                if (!File.Exists(databasePath))
                {
                    AppLogger.Warning($"Backup skipped. Database file not found: {databasePath}");
                    return false;
                }

                var backupLocation = UserPreferencesService.GetBackupLocation();
                if (string.IsNullOrWhiteSpace(backupLocation))
                {
                    backupLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                var backupDirectory = Path.Combine(backupLocation, BackupFolderName);
                Directory.CreateDirectory(backupDirectory);

                var sourceName = Path.GetFileNameWithoutExtension(databasePath);
                var sourceExtension = Path.GetExtension(databasePath);
                if (string.IsNullOrWhiteSpace(sourceExtension))
                {
                    sourceExtension = ".bak";
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{sourceName}_backup_{timestamp}{sourceExtension}";
                var backupPath = Path.Combine(backupDirectory, backupFileName);

                File.Copy(databasePath, backupPath, overwrite: false);
                AppLogger.Success($"Backup created: {backupPath}");

                // Enforce retention
                var retention = UserPreferencesService.GetBackupRetentionCount();
                if (retention > 0)
                {
                    var files = Directory.GetFiles(backupDirectory, $"{sourceName}_backup_*{sourceExtension}");
                    var ordered = files.Select(f => new FileInfo(f)).OrderByDescending(fi => fi.CreationTime).ToList();
                    for (int i = retention; i < ordered.Count; i++)
                    {
                        try
                        {
                            ordered[i].Delete();
                            AppLogger.Info($"Deleted old backup: {ordered[i].FullName}");
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Warning($"Failed to delete old backup {ordered[i].FullName}: {ex.Message}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Backup failed: {ex.Message}");
                return false;
            }
        }

        public void UpdateInterval(TimeSpan newInterval)
        {
            if (newInterval.TotalSeconds <= 0) return;
            _timer.Interval = newInterval;
        }

        public void Dispose()
        {
            _timer.Tick -= OnBackupTick;
            _timer.Stop();
        }
    }
}
