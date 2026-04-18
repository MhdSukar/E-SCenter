using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class PartsRepository
    {
        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;");
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

        public Task<List<PartModel>> GetAllAsync()
            => Task.Run(GetAll);

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

        public Task<List<string>> GetSkusAsync()
            => Task.Run(GetSkus);

        public void DecrementQuantityBySku(string sku, int amount)
        {
            if (string.IsNullOrWhiteSpace(sku) || amount <= 0)
            {
                return;
            }

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                UPDATE Parts
                SET QuantityOnHand = CASE
                    WHEN QuantityOnHand - @amount < 0 THEN 0
                    ELSE QuantityOnHand - @amount
                END
                WHERE SKU = @sku;", conn);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@sku", sku.Trim());
            cmd.ExecuteNonQuery();
        }

        public Task DecrementQuantityBySkuAsync(string sku, int amount)
            => Task.Run(() => DecrementQuantityBySku(sku, amount));

        // -------------------------
        // INSERT
        // -------------------------
        public int Insert(PartModel part)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                            INSERT INTO Parts
                            (SKU, PartCode, PartType, QuantityOnHand, Price, PriceCurrency, QualityGrade, LocationShelf, LocationBin,
                             UnitValue1, UnitCode1, UnitValue2, UnitCode2, ChipPartNumber, Category, Description)
                            VALUES
                            (@SKU, @PartCode, @PartType, @Qty, @Price, @PriceCurrency, @Quality, @Shelf, @Bin,
                             @UnitValue1, @UnitCode1, @UnitValue2, @UnitCode2, @ChipPN, @Category, @Desc);
                            SELECT last_insert_rowid();", conn);

            Bind(cmd, part);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Task<int> InsertAsync(PartModel part)
            => Task.Run(() => Insert(part));

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
                            Price=@Price,
                            PriceCurrency=@PriceCurrency,
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

        public Task UpdateAsync(PartModel part)
            => Task.Run(() => Update(part));

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

        public Task DeleteAsync(int partId)
            => Task.Run(() => Delete(partId));

        // -------------------------
        // Helpers
        // -------------------------
        private static void Bind(SQLiteCommand cmd, PartModel p)
        {
            cmd.Parameters.AddWithValue("@SKU", p.SKU);
            cmd.Parameters.AddWithValue("@PartCode", p.PartCode);
            cmd.Parameters.AddWithValue("@PartType", p.PartType);
            cmd.Parameters.AddWithValue("@Qty", p.QuantityOnHand);
            cmd.Parameters.AddWithValue("@Price", p.Price);
            cmd.Parameters.AddWithValue("@PriceCurrency", p.PriceCurrency ?? "S.P");
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
                Price = Convert.ToDouble(r["Price"]),
                PriceCurrency = r["PriceCurrency"]?.ToString() ?? "S.P",
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
        // -------------------------
        // Restock
        // -------------------------
        public void RestoreQuantityBySku(string sku, int amount)
        {
            if (string.IsNullOrWhiteSpace(sku) || amount <= 0) return;
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE Parts SET QuantityOnHand = QuantityOnHand + @amount WHERE SKU = @sku;", conn);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@sku", sku.Trim());
            cmd.ExecuteNonQuery();
        }

        public Task RestoreQuantityBySkuAsync(string sku, int amount)
            => Task.Run(() => RestoreQuantityBySku(sku, amount));


    }
}
