using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using ESCenter.Core;

namespace ESCenter.Services
{
    public static class CsvExportService
    {
        public static async Task ExportAsync(IEnumerable<object> rows, IEnumerable<string> headers, Func<object, IEnumerable<string>> rowMapper, string defaultFileName)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = defaultFileName,
                    AddExtension = true,
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                await using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write);
                await using var writer = new StreamWriter(stream, new UTF8Encoding(true));

                await writer.WriteLineAsync(string.Join(",", headers.Select(Escape)));
                foreach (var row in rows)
                {
                    var mapped = rowMapper(row) ?? Enumerable.Empty<string>();
                    await writer.WriteLineAsync(string.Join(",", mapped.Select(Escape)));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to export CSV: {ex.Message}");
            }
        }

        private static string Escape(string? value)
        {
            var text = value ?? string.Empty;
            if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
