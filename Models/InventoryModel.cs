using System.Collections.Generic;
using System.Data.SQLite;
using ESCenter.Models;

namespace ESCenter.Data
{
    public class InventoryModel
    {
        private readonly string _connectionString;

        public InventoryModel(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<InventoryItemModel> GetAll()
        {
            var list = new List<InventoryItemModel>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand("SELECT * FROM Inventory ORDER BY ItemType, Brand, Model", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new InventoryItemModel
                {
                    InventoryId = reader.GetInt32(reader.GetOrdinal("InventoryId")),
                    ItemType = reader["ItemType"]?.ToString(),
                    Brand = reader["Brand"]?.ToString(),
                    Model = reader["Model"]?.ToString(),
                    Variant = reader["Variant"]?.ToString(),
                    Compatibility = reader["Compatibility"]?.ToString(),
                    Specs = reader["Specs"]?.ToString(),
                    Size = reader["Size"]?.ToString(),
                    QuantityOnHand = reader["QuantityOnHand"] != DBNull.Value ? int.Parse(reader["QuantityOnHand"].ToString()) : 0,
                    Condition = reader["Condition"]?.ToString(),
                    QualityGrade = reader["QualityGrade"] != DBNull.Value ? int.Parse(reader["QualityGrade"].ToString()) : 3,
                    Source = reader["Source"]?.ToString(),
                    LocationBox = reader["LocationBox"]?.ToString(),
                    Description = reader["Description"]?.ToString(),
                    Notes = reader["Notes"]?.ToString(),
                    Tags = reader["Tags"]?.ToString()
                });
            }

            return list;
        }

        public void Insert(InventoryItemModel item)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(@"
            INSERT INTO Inventory
            (ItemType, Brand, Model, Variant, Compatibility, Specs, Size, QuantityOnHand, Condition, QualityGrade, Source, LocationBox, Description, Notes, Tags)
            VALUES
            (@ItemType, @Brand, @Model, @Variant, @Compatibility, @Specs, @Size, @QuantityOnHand, @Condition, @QualityGrade, @Source, @LocationBox, @Description, @Notes, @Tags)", conn);

            BindParams(cmd, item);
            cmd.ExecuteNonQuery();
        }

        public void Update(InventoryItemModel item)
        {
            using var conn = new SQLiteConnection(_connectionString);
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
            QuantityOnHand=@QuantityOnHand,
            Condition=@Condition,
            QualityGrade=@QualityGrade,
            Source=@Source,
            LocationBox=@LocationBox,
            Description=@Description,
            Notes=@Notes,
            Tags=@Tags
            WHERE InventoryId=@InventoryId", conn);

            BindParams(cmd, item);
            cmd.Parameters.AddWithValue("@InventoryId", item.InventoryId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int inventoryId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand("DELETE FROM Inventory WHERE InventoryId=@id", conn);
            cmd.Parameters.AddWithValue("@id", inventoryId);
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
            cmd.Parameters.AddWithValue("@QuantityOnHand", item.QuantityOnHand);
            cmd.Parameters.AddWithValue("@Condition", item.Condition);
            cmd.Parameters.AddWithValue("@QualityGrade", item.QualityGrade);
            cmd.Parameters.AddWithValue("@Source", item.Source);
            cmd.Parameters.AddWithValue("@LocationBox", item.LocationBox);
            cmd.Parameters.AddWithValue("@Description", item.Description);
            cmd.Parameters.AddWithValue("@Notes", item.Notes);
            cmd.Parameters.AddWithValue("@Tags", item.Tags);
        }
    }
}
