using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class InventoryRepository
    {
        private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        public List<InventoryItemModel> GetAll()
        {
            var items = new List<InventoryItemModel>();

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand("SELECT * FROM Inventory;", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new InventoryItemModel
                {
                    InventoryId = reader.GetInt32(reader.GetOrdinal("InventoryId")),
                    ItemType = reader["ItemType"]?.ToString(),
                    Brand = reader["Brand"]?.ToString(),
                    Model = reader["Model"]?.ToString(),
                    Variant = reader["Variant"]?.ToString(),
                    Compatibility = reader["Compatibility"]?.ToString(),
                    Specs = reader["Specs"]?.ToString(),
                    Size = reader["Size"]?.ToString(),
                    QuantityOnHand = reader.GetInt32(reader.GetOrdinal("QuantityOnHand")),
                    Price = reader.GetDouble(reader.GetOrdinal("Price")),
                    Condition = reader["Condition"]?.ToString(),
                    QualityGrade = reader.GetInt32(reader.GetOrdinal("QualityGrade")),
                    Source = reader["Source"]?.ToString(),
                    LocationBox = reader["LocationBox"]?.ToString(),
                    Description = reader["Description"]?.ToString(),
                    Notes = reader["Notes"]?.ToString(),
                    Tags = reader["Tags"]?.ToString()
                });
            }

            return items;
        }

        public List<string> GetNames()
        {
            var items = new List<string>();

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(
                "SELECT ItemType, Brand, Model, Description FROM Inventory;", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var description = reader["Description"]?.ToString();
                var name = !string.IsNullOrWhiteSpace(description)
                    ? description.Trim()
                    : BuildName(reader["ItemType"]?.ToString(), reader["Brand"]?.ToString(), reader["Model"]?.ToString());

                if (!string.IsNullOrWhiteSpace(name))
                {
                    items.Add(name);
                }
            }

            return items;
        }

        public void DecrementQuantityByName(string name, int amount)
        {
            if (string.IsNullOrWhiteSpace(name) || amount <= 0)
            {
                return;
            }

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                UPDATE Inventory
                SET QuantityOnHand = CASE
                    WHEN QuantityOnHand - @amount < 0 THEN 0
                    ELSE QuantityOnHand - @amount
                END
                WHERE Description = @name
                   OR (Description IS NULL AND
                       TRIM(COALESCE(ItemType, '') || ' ' || COALESCE(Brand, '') || ' ' || COALESCE(Model, '')) = @name);", conn);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.ExecuteNonQuery();
        }

        public void Insert(InventoryItemModel item)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
             INSERT INTO Inventory
             (ItemType, Brand, Model, Variant, Compatibility, Specs, Size, QuantityOnHand, Price, Condition, QualityGrade, Source, LocationBox, Description, Notes, Tags)
             VALUES
             (@ItemType, @Brand, @Model, @Variant, @Compatibility, @Specs, @Size, @Quantity, @Price, @Condition, @Quality, @Source, @LocationBox, @Description, @Notes, @Tags);", conn);

            BindParams(cmd, item);
            cmd.ExecuteNonQuery();
        }

        public void Update(InventoryItemModel item)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                            UPDATE Inventory SET
                            ItemType=@ItemType,
                            Brand=@Brand,
                            Model=@Model,
                            Variant=@Variant,
                            Compatibility=@Compatibility,
                            Specs=@Specs,
                            Size=@Size,
                            QuantityOnHand=@Quantity,
                            Price=@Price,
                            Condition=@Condition,
                            QualityGrade=@Quality,
                            Source=@Source,
                            LocationBox=@LocationBox,
                            Description=@Description,
                            Notes=@Notes,
                            Tags=@Tags
                            WHERE InventoryId=@Id;", conn);

            BindParams(cmd, item);
            cmd.Parameters.AddWithValue("@Id", item.InventoryId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand("DELETE FROM Inventory WHERE InventoryId=@Id;", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        private void BindParams(SQLiteCommand cmd, InventoryItemModel item)
        {
            cmd.Parameters.AddWithValue("@ItemType", item.ItemType);
            cmd.Parameters.AddWithValue("@Brand", item.Brand);
            cmd.Parameters.AddWithValue("@Model", item.Model);
            cmd.Parameters.AddWithValue("@Variant", item.Variant);
            cmd.Parameters.AddWithValue("@Compatibility", item.Compatibility);
            cmd.Parameters.AddWithValue("@Specs", item.Specs);
            cmd.Parameters.AddWithValue("@Size", item.Size);
            cmd.Parameters.AddWithValue("@Quantity", item.QuantityOnHand);
            cmd.Parameters.AddWithValue("@Price", item.Price);
            cmd.Parameters.AddWithValue("@Condition", item.Condition);
            cmd.Parameters.AddWithValue("@Quality", item.QualityGrade);
            cmd.Parameters.AddWithValue("@Source", item.Source);
            cmd.Parameters.AddWithValue("@LocationBox", item.LocationBox);
            cmd.Parameters.AddWithValue("@Description", item.Description);
            cmd.Parameters.AddWithValue("@Notes", item.Notes);
            cmd.Parameters.AddWithValue("@Tags", item.Tags);
        }

        private static string BuildName(string itemType, string brand, string model)
        {
            var parts = new[] { itemType, brand, model }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim());
            return string.Join(" ", parts);
        }
    }
}
