using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ESCenter.Core
{
    /// <summary>
    /// Writes every AppLogger entry to a daily rolling log file in AppData.
    /// Call FileLogger.Initialize() once on application startup.
    /// Never throws — all operations are best-effort.
    /// </summary>
    public static class FileLogger
    {
        private static string _logDirectory = string.Empty;
        private static readonly object _writeLock = new object();
        private static bool _initialized;

        /// <summary>
        /// Initializes the file logger. Must be called once at startup
        /// before any AppLogger calls are made.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                _logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ESCenter", "Logs");

                Directory.CreateDirectory(_logDirectory);
            }
            catch
            {
                // If we can't create the log directory, silently give up.
                // The app must never crash because of logging.
                _logDirectory = string.Empty;
                return;
            }

            // Subscribe to AppLogger's internal hook
            AppLogger.LogEntryRaised += (message, level) =>
                _ = WriteAsync(message, level);

            // Clean up log files older than 7 days — fire and forget
            _ = Task.Run(CleanOldLogs);
        }

        /// <summary>
        /// Path to today's log file. Null if FileLogger was not initialized
        /// or the log directory could not be created.
        /// </summary>
        public static string? TodayLogPath
        {
            get
            {
                if (string.IsNullOrEmpty(_logDirectory)) return null;
                return Path.Combine(_logDirectory,
                    $"escenter-{DateTime.Today:yyyy-MM-dd}.log");
            }
        }

        /// <summary>
        /// The directory where log files are stored.
        /// Null if not initialized or creation failed.
        /// </summary>
        public static string? LogDirectory =>
            string.IsNullOrEmpty(_logDirectory) ? null : _logDirectory;

        // ── Private helpers ───────────────────────────────────────────────

        private static async Task WriteAsync(string message, StatusLevel level)
        {
            if (string.IsNullOrEmpty(_logDirectory)) return;

            var timestamp = DateTime.Now;
            var fileName  = $"escenter-{timestamp:yyyy-MM-dd}.log";
            var filePath  = Path.Combine(_logDirectory, fileName);

            // Format: [HH:mm:ss.fff] [LEVEL  ] message
            var levelPadded = level.ToString().ToUpperInvariant().PadRight(7);
            var line = $"[{timestamp:HH:mm:ss.fff}] [{levelPadded}] {message}{Environment.NewLine}";

            await Task.Run(() =>
            {
                try
                {
                    lock (_writeLock)
                    {
                        File.AppendAllText(filePath, line, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Never propagate logging exceptions to the UI thread.
                }
            });
        }

        private static void CleanOldLogs()
        {
            if (string.IsNullOrEmpty(_logDirectory)) return;

            try
            {
                var cutoff = DateTime.Today.AddDays(-7);
                foreach (var file in Directory.GetFiles(
                    _logDirectory, "escenter-*.log",
                    SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) < cutoff)
                            File.Delete(file);
                    }
                    catch { /* skip files that are locked or missing */ }
                }
            }
            catch { /* best-effort: never crash on log cleanup */ }
        }
    }
}
