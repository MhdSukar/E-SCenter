using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using ESCenter.Models;

namespace ESCenter.Data
{
    public class PartsRepository
    {
        private readonly string _dbPath;

        public PartsRepository()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
        }

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        }

        // -------------------------
        // READ
        // -------------------------
        public List<PartModel> GetAll()
        {
            var result = new List<PartModel>();

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand("SELECT * FROM Parts;", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(Map(reader));
            }

            return result;
        }

        public List<string> GetSkus()
        {
            var result = new List<string>();

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand("SELECT SKU FROM Parts WHERE SKU IS NOT NULL AND TRIM(SKU) <> '';", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var sku = reader["SKU"]?.ToString();
                if (!string.IsNullOrWhiteSpace(sku))
                {
                    result.Add(sku.Trim());
                }
            }

            return result;
        }

        // -------------------------
        // INSERT
        // -------------------------
        public int Insert(PartModel part)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                            INSERT INTO Parts
                            (SKU, PartCode, PartType, QuantityOnHand, QualityGrade, LocationShelf, LocationBin,
                             UnitValue1, UnitCode1, UnitValue2, UnitCode2, ChipPartNumber, Category, Description)
                            VALUES
                            (@SKU, @PartCode, @PartType, @Qty, @Quality, @Shelf, @Bin,
                             @UnitValue1, @UnitCode1, @UnitValue2, @UnitCode2, @ChipPN, @Category, @Desc);
                            SELECT last_insert_rowid();", conn);

            Bind(cmd, part);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // -------------------------
        // UPDATE
        // -------------------------
        public void Update(PartModel part)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                            UPDATE Parts SET
                            SKU=@SKU,
                            PartCode=@PartCode,
                            PartType=@PartType,
                            QuantityOnHand=@Qty,
                            QualityGrade=@Quality,
                            LocationShelf=@Shelf,
                            LocationBin=@Bin,
                            UnitValue1=@UnitValue1,
                            UnitCode1=@UnitCode1,
                            UnitValue2=@UnitValue2,
                            UnitCode2=@UnitCode2,
                            ChipPartNumber=@ChipPN,
                            Category=@Category,
                            Description=@Desc
                            WHERE PartId=@Id;", conn);

            Bind(cmd, part);
            cmd.Parameters.AddWithValue("@Id", part.PartId);
            cmd.ExecuteNonQuery();
        }

        // -------------------------
        // DELETE
        // -------------------------
        public void Delete(int partId)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand("DELETE FROM Parts WHERE PartId=@Id;", conn);
            cmd.Parameters.AddWithValue("@Id", partId);
            cmd.ExecuteNonQuery();
        }

        // -------------------------
        // Helpers
        // -------------------------
        private static void Bind(SQLiteCommand cmd, PartModel p)
        {
            cmd.Parameters.AddWithValue("@SKU", p.SKU);
            cmd.Parameters.AddWithValue("@PartCode", p.PartCode);
            cmd.Parameters.AddWithValue("@PartType", p.PartType);
            cmd.Parameters.AddWithValue("@Qty", p.QuantityOnHand);
            cmd.Parameters.AddWithValue("@Quality", p.QualityGrade);
            cmd.Parameters.AddWithValue("@Shelf", p.LocationShelf);
            cmd.Parameters.AddWithValue("@Bin", p.LocationBin);
            cmd.Parameters.AddWithValue("@UnitValue1", p.UnitValue1);
            cmd.Parameters.AddWithValue("@UnitCode1", p.UnitCode1);
            cmd.Parameters.AddWithValue("@UnitValue2", p.UnitValue2);
            cmd.Parameters.AddWithValue("@UnitCode2", p.UnitCode2);
            cmd.Parameters.AddWithValue("@ChipPN", p.ChipPartNumber);
            cmd.Parameters.AddWithValue("@Category", p.Category);
            cmd.Parameters.AddWithValue("@Desc", p.Description);
        }

        private static PartModel Map(SQLiteDataReader r)
        {
            return new PartModel
            {
                PartId = Convert.ToInt32(r["PartId"]),
                SKU = r["SKU"]?.ToString(),
                PartCode = r["PartCode"]?.ToString(),
                PartType = r["PartType"]?.ToString(),
                QuantityOnHand = Convert.ToInt32(r["QuantityOnHand"]),
                QualityGrade = Convert.ToInt32(r["QualityGrade"]),
                LocationShelf = r["LocationShelf"]?.ToString(),
                LocationBin = r["LocationBin"]?.ToString(),
                UnitValue1 = Convert.ToDouble(r["UnitValue1"]),
                UnitCode1 = r["UnitCode1"]?.ToString(),
                UnitValue2 = Convert.ToDouble(r["UnitValue2"]),
                UnitCode2 = r["UnitCode2"]?.ToString(),
                ChipPartNumber = r["ChipPartNumber"]?.ToString(),
                Category = r["Category"]?.ToString(),
                Description = r["Description"]?.ToString()
            };
        }
    }
}
