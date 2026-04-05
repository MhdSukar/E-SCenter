using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Threading.Tasks;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class TicketStatusHistoryRepository
    {
        private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        public void Insert(TicketStatusEntry entry)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            const string sql = """
                INSERT INTO TicketStatusHistory
                (TicketId, OldStatus, NewStatus, Note, ChangedAt)
                VALUES
                (@TicketId, @OldStatus, @NewStatus, @Note, @ChangedAt);
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TicketId", entry.TicketId);
            cmd.Parameters.AddWithValue("@OldStatus", (object)entry.OldStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewStatus", entry.NewStatus);
            cmd.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(entry.Note) ? DBNull.Value : (object)entry.Note);
            cmd.Parameters.AddWithValue("@ChangedAt", entry.ChangedAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public List<TicketStatusEntry> GetByTicketId(int ticketId)
        {
            var list = new List<TicketStatusEntry>();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            const string sql = """
                SELECT HistoryId, TicketId, OldStatus, NewStatus, Note, ChangedAt
                FROM TicketStatusHistory
                WHERE TicketId = @id
                ORDER BY ChangedAt ASC;
            """;

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", ticketId);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                var changedAtText = r["ChangedAt"]?.ToString() ?? DateTime.Now.ToString("o");
                list.Add(new TicketStatusEntry
                {
                    HistoryId = Convert.ToInt32(r["HistoryId"]),
                    TicketId = Convert.ToInt32(r["TicketId"]),
                    OldStatus = r["OldStatus"]?.ToString(),
                    NewStatus = r["NewStatus"]?.ToString(),
                    Note = r["Note"]?.ToString(),
                    ChangedAt = DateTime.Parse(changedAtText, null, DateTimeStyles.RoundtripKind)
                });
            }

            return list;
        }

        public Task InsertAsync(TicketStatusEntry entry)
            => Task.Run(() => Insert(entry));

        public Task<List<TicketStatusEntry>> GetByTicketIdAsync(int ticketId)
            => Task.Run(() => GetByTicketId(ticketId));
    }
}
