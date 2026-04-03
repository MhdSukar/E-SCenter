using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ESCenter.Models;

namespace ESCenter.Services
{
    public class AlBarakaDataService
    {
        private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        public List<AlBarakaRecord> GetAll()
        {
            var list = new List<AlBarakaRecord>();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var hasAccountColumn = HasColumn(conn, "AlBaraka", "Account");
            var sql = hasAccountColumn
                ? @"SELECT AlBarakaId, Date, ItemName, Price, PriceCurrency, Category, Account, CreatedAt, UpdatedAt FROM AlBaraka ORDER BY Date DESC;"
                : @"SELECT AlBarakaId, Date, ItemName, Price, PriceCurrency, Category, '' AS Account, CreatedAt, UpdatedAt FROM AlBaraka ORDER BY Date DESC;";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public Task<List<AlBarakaRecord>> GetAllAsync()
            => Task.Run(GetAll);

        /// <summary>
        /// Get totals grouped by currency for records optionally within a date range.
        /// Date column stored as yyyy-MM-dd so string comparison works for ISO dates.
        /// </summary>
        public (decimal TotalSP, decimal TotalUSD) GetTotals(DateTime? start = null, DateTime? end = null)
        {
            decimal totalSP = 0m;
            decimal totalUSD = 0m;

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = "SELECT PriceCurrency, SUM(Price) as Total FROM AlBaraka";
            var where = new List<string>();
            if (start.HasValue)
            {
                where.Add("Date >= @start");
            }
            if (end.HasValue)
            {
                where.Add("Date <= @end");
            }
            if (where.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", where);
            }
            sql += " GROUP BY PriceCurrency;";

            using var cmd = new SQLiteCommand(sql, conn);
            if (start.HasValue)
            {
                cmd.Parameters.AddWithValue("@start", start.Value.ToString("yyyy-MM-dd"));
            }
            if (end.HasValue)
            {
                cmd.Parameters.AddWithValue("@end", end.Value.ToString("yyyy-MM-dd"));
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var cur = reader["PriceCurrency"]?.ToString() ?? string.Empty;
                var tot = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0m;
                if (string.Equals(cur, "S.P", StringComparison.OrdinalIgnoreCase))
                {
                    totalSP = tot;
                }
                else if (string.Equals(cur, "USD", StringComparison.OrdinalIgnoreCase))
                {
                    totalUSD = tot;
                }
            }

            return (totalSP, totalUSD);
        }

        public Task<(decimal TotalSP, decimal TotalUSD)> GetTotalsAsync(DateTime? start = null, DateTime? end = null)
            => Task.Run(() => GetTotals(start, end));

        public int Insert(AlBarakaRecord rec)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var hasAccountColumn = HasColumn(conn, "AlBaraka", "Account");
            var sql = hasAccountColumn
                ? @"INSERT INTO AlBaraka (Date, ItemName, Price, PriceCurrency, Category, Account) VALUES (@date, @item, @price, @currency, @category, @account); SELECT last_insert_rowid();"
                : @"INSERT INTO AlBaraka (Date, ItemName, Price, PriceCurrency, Category) VALUES (@date, @item, @price, @currency, @category); SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@date", rec.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@item", rec.ItemName ?? string.Empty);
            cmd.Parameters.AddWithValue("@price", rec.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@currency", rec.PriceCurrency ?? "S.P");
            cmd.Parameters.AddWithValue("@category", rec.Category ?? string.Empty);
            if (hasAccountColumn)
            {
                cmd.Parameters.AddWithValue("@account", rec.Account?.Trim() ?? string.Empty);
            }

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Task<int> InsertAsync(AlBarakaRecord rec)
            => Task.Run(() => Insert(rec));

        public void Update(AlBarakaRecord rec)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var hasAccountColumn = HasColumn(conn, "AlBaraka", "Account");
            var sql = hasAccountColumn
                ? @"UPDATE AlBaraka SET Date=@date, ItemName=@item, Price=@price, PriceCurrency=@currency, Category=@category, Account=@account, UpdatedAt=CURRENT_TIMESTAMP WHERE AlBarakaId=@id;"
                : @"UPDATE AlBaraka SET Date=@date, ItemName=@item, Price=@price, PriceCurrency=@currency, Category=@category, UpdatedAt=CURRENT_TIMESTAMP WHERE AlBarakaId=@id;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", rec.AlBarakaId);
            cmd.Parameters.AddWithValue("@date", rec.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@item", rec.ItemName ?? string.Empty);
            cmd.Parameters.AddWithValue("@price", rec.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@currency", rec.PriceCurrency ?? "S.P");
            cmd.Parameters.AddWithValue("@category", rec.Category ?? string.Empty);
            if (hasAccountColumn)
            {
                cmd.Parameters.AddWithValue("@account", rec.Account?.Trim() ?? string.Empty);
            }

            cmd.ExecuteNonQuery();
        }

        public Task UpdateAsync(AlBarakaRecord rec)
            => Task.Run(() => Update(rec));

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM AlBaraka WHERE AlBarakaId=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public Task DeleteAsync(int id)
            => Task.Run(() => Delete(id));

        private AlBarakaRecord Map(SQLiteDataReader reader)
        {
            var dateStr = reader["Date"]?.ToString() ?? string.Empty;
            DateTime parsedDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    if (!DateTime.TryParseExact(dateStr, "o", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out parsedDate))
                    {
                        DateTime.TryParse(dateStr, out parsedDate);
                    }
                }
            }

            return new AlBarakaRecord
            {
                AlBarakaId = Convert.ToInt32(reader["AlBarakaId"]),
                Date = parsedDate,
                ItemName = reader["ItemName"]?.ToString() ?? string.Empty,
                Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : (decimal?)null,
                PriceCurrency = reader["PriceCurrency"]?.ToString() ?? "S.P",
                Category = reader["Category"]?.ToString() ?? string.Empty,
                Account = reader["Account"]?.ToString() ?? string.Empty,
                CreatedAt = reader["CreatedAt"]?.ToString() ?? string.Empty,
                UpdatedAt = reader["UpdatedAt"]?.ToString() ?? string.Empty
            };
        }

        private static bool HasColumn(SQLiteConnection conn, string tableName, string columnName)
        {
            using var cmd = new SQLiteCommand($"PRAGMA table_info({tableName});", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var currentName = reader["name"]?.ToString();
                if (string.Equals(currentName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
