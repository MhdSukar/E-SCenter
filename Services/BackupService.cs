using System;
using System.IO;
using ESCenter.Core;
using System.Windows.Threading;

namespace ESCenter.Services
{
    public sealed class BackupService : IDisposable
    {
        private const string BackupFolderName = "ESCenter Backups";
        private static readonly TimeSpan BackupInterval = TimeSpan.FromHours(1);

        private readonly DispatcherTimer _timer;

        public BackupService()
        {
            _timer = new DispatcherTimer
            {
                Interval = BackupInterval
            };

            _timer.Tick += OnBackupTick;
        }

        public void Start()
        {
            _timer.Start();
            AppLogger.Info("Automatic backup service started (every 6 hours). Destination: Documents.");
            TryCreateBackup();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void OnBackupTick(object? sender, EventArgs e)
        {
            TryCreateBackup();
        }

        private void TryCreateBackup()
        {
            try
            {
                var databasePath = DatabasePathService.CurrentDatabasePath;
                if (!File.Exists(databasePath))
                {
                    AppLogger.Warning($"Automatic backup skipped. Database file not found: {databasePath}");
                    return;
                }

                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var backupDirectory = Path.Combine(documentsPath, BackupFolderName);
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
                AppLogger.Success($"Automatic backup created: {backupPath}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Automatic backup failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer.Tick -= OnBackupTick;
            _timer.Stop();
        }
    }
}
