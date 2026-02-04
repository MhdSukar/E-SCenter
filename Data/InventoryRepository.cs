using System.Collections.Generic;
using System.Data.SQLite;
using ESCenter.Models;

namespace ESCenter.Data
{
    public class InventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
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

        public void Insert(InventoryItemModel item)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(@"
             INSERT INTO Inventory
             (ItemType, Brand, Model, Variant, Compatibility, Specs, Size, QuantityOnHand, Condition, QualityGrade, Source, LocationBox, Description, Notes, Tags)
             VALUES
             (@ItemType, @Brand, @Model, @Variant, @Compatibility, @Specs, @Size, @Quantity, @Condition, @Quality, @Source, @LocationBox, @Description, @Notes, @Tags);", conn);

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
            cmd.Parameters.AddWithValue("@Condition", item.Condition);
            cmd.Parameters.AddWithValue("@Quality", item.QualityGrade);
            cmd.Parameters.AddWithValue("@Source", item.Source);
            cmd.Parameters.AddWithValue("@LocationBox", item.LocationBox);
            cmd.Parameters.AddWithValue("@Description", item.Description);
            cmd.Parameters.AddWithValue("@Notes", item.Notes);
            cmd.Parameters.AddWithValue("@Tags", item.Tags);
        }
    }
}
