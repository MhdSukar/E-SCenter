using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class CategoryRepository
    {
        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;");
        }

        public List<string> GetByType(string forType)
        {
            var result = new List<string>();
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                SELECT CategoryName FROM PartCategories
                WHERE ForType = @forType ORDER BY CategoryName ASC;", conn);
            cmd.Parameters.AddWithValue("@forType", forType);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader["CategoryName"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        public Task<List<string>> GetByTypeAsync(string forType) => Task.Run(() => GetByType(forType));

        public void Insert(string categoryName, string forType)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                INSERT OR IGNORE INTO PartCategories (CategoryName, ForType)
                VALUES (@name, @type);", conn);
            cmd.Parameters.AddWithValue("@name", categoryName);
            cmd.Parameters.AddWithValue("@type", forType);
            cmd.ExecuteNonQuery();
        }

        public Task InsertAsync(string categoryName, string forType) => Task.Run(() => Insert(categoryName, forType));

        public void Delete(string categoryName, string forType)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                DELETE FROM PartCategories
                WHERE CategoryName = @name AND ForType = @type;", conn);
            cmd.Parameters.AddWithValue("@name", categoryName);
            cmd.Parameters.AddWithValue("@type", forType);
            cmd.ExecuteNonQuery();
        }

        public Task DeleteAsync(string categoryName, string forType) => Task.Run(() => Delete(categoryName, forType));

        public bool Exists(string categoryName, string forType)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                SELECT COUNT(1) FROM PartCategories
                WHERE LOWER(CategoryName)=LOWER(@name) AND ForType=@type;", conn);
            cmd.Parameters.AddWithValue("@name", categoryName);
            cmd.Parameters.AddWithValue("@type", forType);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public Task<bool> ExistsAsync(string categoryName, string forType) => Task.Run(() => Exists(categoryName, forType));
    }
}
