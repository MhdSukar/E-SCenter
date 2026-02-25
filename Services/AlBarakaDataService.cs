using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

            var sql = @"SELECT AlBarakaId, Date, ItemName, Price, PriceCurrency, Category, Account, CreatedAt, UpdatedAt FROM AlBaraka ORDER BY Date DESC;";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public int Insert(AlBarakaRecord rec)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = @"INSERT INTO AlBaraka (Date, ItemName, Price, PriceCurrency, Category, Account) VALUES (@date, @item, @price, @currency, @category, @account); SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn);
            // Store date in ISO yyyy-MM-dd format (date-only)
            cmd.Parameters.AddWithValue("@date", rec.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@item", rec.ItemName ?? string.Empty);
            cmd.Parameters.AddWithValue("@price", rec.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@currency", rec.PriceCurrency ?? "S.P");
            cmd.Parameters.AddWithValue("@category", rec.Category ?? string.Empty);
            cmd.Parameters.AddWithValue("@account", rec.Account ?? string.Empty);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Update(AlBarakaRecord rec)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = @"UPDATE AlBaraka SET Date=@date, ItemName=@item, Price=@price, PriceCurrency=@currency, Category=@category, Account=@account, UpdatedAt=CURRENT_TIMESTAMP WHERE AlBarakaId=@id;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", rec.AlBarakaId);
            // Store date in ISO yyyy-MM-dd format (date-only)
            cmd.Parameters.AddWithValue("@date", rec.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@item", rec.ItemName ?? string.Empty);
            cmd.Parameters.AddWithValue("@price", rec.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@currency", rec.PriceCurrency ?? "S.P");
            cmd.Parameters.AddWithValue("@category", rec.Category ?? string.Empty);
            cmd.Parameters.AddWithValue("@account", rec.Account ?? string.Empty);

            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM AlBaraka WHERE AlBarakaId=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private AlBarakaRecord Map(SQLiteDataReader reader)
        {
            // Date stored as yyyy-MM-dd (date-only); accept older formats as fallback
            var dateStr = reader["Date"]?.ToString() ?? string.Empty;
            DateTime parsedDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                // Try exact parse for yyyy-MM-dd first
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    // Fallback to round-trip or general parse
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
    }
}
