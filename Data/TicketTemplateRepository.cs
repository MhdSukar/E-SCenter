using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class TicketTemplateRepository
    {
        private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        public List<TicketTemplate> GetAll()
        {
            var list = new List<TicketTemplate>();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            const string sql = """
                SELECT TemplateId, Name, DeviceCategory, DeviceBrand, DeviceModel, ProblemDescription,
                       Notes, PriorityLevel, EstimatedCost, EstimatedCostCurrency, PartsUsed, CreatedAt
                FROM TicketTemplates
                ORDER BY TemplateId DESC;
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TicketTemplate
                {
                    TemplateId = Convert.ToInt32(reader["TemplateId"]),
                    Name = reader["Name"]?.ToString() ?? string.Empty,
                    DeviceCategory = reader["DeviceCategory"]?.ToString() ?? string.Empty,
                    DeviceBrand = reader["DeviceBrand"]?.ToString() ?? string.Empty,
                    DeviceModel = reader["DeviceModel"]?.ToString() ?? string.Empty,
                    ProblemDescription = reader["ProblemDescription"]?.ToString() ?? string.Empty,
                    Notes = reader["Notes"]?.ToString() ?? string.Empty,
                    PriorityLevel = reader["PriorityLevel"]?.ToString() ?? "Normal",
                    EstimatedCost = reader["EstimatedCost"] == DBNull.Value ? null : Convert.ToDecimal(reader["EstimatedCost"]),
                    EstimatedCostCurrency = reader["EstimatedCostCurrency"]?.ToString() ?? "S.P",
                    PartsUsed = reader["PartsUsed"]?.ToString() ?? string.Empty,
                    CreatedAt = reader["CreatedAt"]?.ToString() ?? string.Empty
                });
            }

            return list;
        }

        public Task<List<TicketTemplate>> GetAllAsync()
            => Task.Run(GetAll);

        public int Insert(TicketTemplate t)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            const string sql = """
                INSERT INTO TicketTemplates
                (Name, DeviceCategory, DeviceBrand, DeviceModel, ProblemDescription, Notes, PriorityLevel,
                 EstimatedCost, EstimatedCostCurrency, PartsUsed)
                VALUES
                (@Name, @DeviceCategory, @DeviceBrand, @DeviceModel, @ProblemDescription, @Notes, @PriorityLevel,
                 @EstimatedCost, @EstimatedCostCurrency, @PartsUsed);
                SELECT last_insert_rowid();
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", t.Name);
            cmd.Parameters.AddWithValue("@DeviceCategory", string.IsNullOrWhiteSpace(t.DeviceCategory) ? DBNull.Value : (object)t.DeviceCategory);
            cmd.Parameters.AddWithValue("@DeviceBrand", string.IsNullOrWhiteSpace(t.DeviceBrand) ? DBNull.Value : (object)t.DeviceBrand);
            cmd.Parameters.AddWithValue("@DeviceModel", string.IsNullOrWhiteSpace(t.DeviceModel) ? DBNull.Value : (object)t.DeviceModel);
            cmd.Parameters.AddWithValue("@ProblemDescription", string.IsNullOrWhiteSpace(t.ProblemDescription) ? DBNull.Value : (object)t.ProblemDescription);
            cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(t.Notes) ? DBNull.Value : (object)t.Notes);
            cmd.Parameters.AddWithValue("@PriorityLevel", string.IsNullOrWhiteSpace(t.PriorityLevel) ? "Normal" : t.PriorityLevel);
            cmd.Parameters.AddWithValue("@EstimatedCost", t.EstimatedCost.HasValue ? (object)t.EstimatedCost.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@EstimatedCostCurrency", string.IsNullOrWhiteSpace(t.EstimatedCostCurrency) ? "S.P" : t.EstimatedCostCurrency);
            cmd.Parameters.AddWithValue("@PartsUsed", string.IsNullOrWhiteSpace(t.PartsUsed) ? DBNull.Value : (object)t.PartsUsed);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Task<int> InsertAsync(TicketTemplate t)
            => Task.Run(() => Insert(t));

        public void Delete(int templateId)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            using var cmd = new SQLiteCommand("DELETE FROM TicketTemplates WHERE TemplateId = @id;", conn);
            cmd.Parameters.AddWithValue("@id", templateId);
            cmd.ExecuteNonQuery();
        }

        public Task DeleteAsync(int templateId)
            => Task.Run(() => Delete(templateId));
    }
}
