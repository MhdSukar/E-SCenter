using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using ESCenter.Models;

namespace ESCenter.Data
{
    public class BoneyardRepository
    {
        private readonly string _connectionString;

        public BoneyardRepository()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        public List<BoneyardModel> GetAll()
        {
            var list = new List<BoneyardModel>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            const string sql = """
                SELECT DeviceId, DeviceType, Brand, Model, Condition, HolderID, Notes, Price, AddedAt
                FROM Boneyard
                ORDER BY DeviceId DESC;
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new BoneyardModel
                {
                    DeviceId = Convert.ToInt32(r["DeviceId"]),
                    DeviceType = Convert.ToInt32(r["DeviceType"]),
                    Brand = r["Brand"]?.ToString(),
                    Model = r["Model"]?.ToString(),
                    Condition = r["Condition"]?.ToString(),
                    HolderID = r["HolderID"]?.ToString(),
                    Notes = r["Notes"]?.ToString(),
                    Price = Convert.ToDouble(r["Price"]),
                    AddedAt = r["AddedAt"]?.ToString()
                });
            }

            return list;
        }

        public int Insert(BoneyardModel d)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            const string sql = """
                INSERT INTO Boneyard
                (DeviceType, Brand, Model, Condition, HolderID, Notes, Price, AddedAt)
                VALUES
                (@DeviceType, @Brand, @Model, @Condition, @HolderID, @Notes, @Price, @AddedAt);
                SELECT last_insert_rowid();
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeviceType", d.DeviceType);
            cmd.Parameters.AddWithValue("@Brand", d.Brand);
            cmd.Parameters.AddWithValue("@Model", d.Model);
            cmd.Parameters.AddWithValue("@Condition", d.Condition);
            cmd.Parameters.AddWithValue("@HolderID", d.HolderID);
            cmd.Parameters.AddWithValue("@Notes", d.Notes);
            cmd.Parameters.AddWithValue("@Price", d.Price);
            cmd.Parameters.AddWithValue("@AddedAt", d.AddedAt);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Update(BoneyardModel d)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            const string sql = """
                UPDATE Boneyard SET
                    DeviceType = @DeviceType,
                    Brand = @Brand,
                    Model = @Model,
                    Condition = @Condition,
                    HolderID = @HolderID,
                    Notes = @Notes,
                    Price = @Price,
                    AddedAt = @AddedAt
                WHERE DeviceId = @DeviceId;
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeviceType", d.DeviceType);
            cmd.Parameters.AddWithValue("@Brand", d.Brand);
            cmd.Parameters.AddWithValue("@Model", d.Model);
            cmd.Parameters.AddWithValue("@Condition", d.Condition);
            cmd.Parameters.AddWithValue("@HolderID", d.HolderID);
            cmd.Parameters.AddWithValue("@Notes", d.Notes);
            cmd.Parameters.AddWithValue("@Price", d.Price);
            cmd.Parameters.AddWithValue("@AddedAt", d.AddedAt);
            cmd.Parameters.AddWithValue("@DeviceId", d.DeviceId);

            cmd.ExecuteNonQuery();
        }

        public void Delete(int deviceId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            const string sql = "DELETE FROM Boneyard WHERE DeviceId = @id;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", deviceId);
            cmd.ExecuteNonQuery();
        }
    }
}
