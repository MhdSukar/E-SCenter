using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using ESCenter.Models;

namespace ESCenter.Services
{
    public class TicketsDataService
    {
        private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        // ===================== GET ALL =====================
        public List<RepairTicket> GetAll()
        {
            var list = new List<RepairTicket>();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = @"
                SELECT TicketId, EscTicketId, CustomerId, CustomerName, PhoneNumber, ContactMethod,
                       DeviceCategory, DeviceBrand, DeviceModel, SerialIMEI, DamageHistory, BoardModifications,
                       ProblemDescription, Notes, RepairStatus, PriorityLevel,
                       EstimatedCost, EstimatedCostCurrency,
                       FinalCost, FinalCostCurrency,
                       RootCause, PartsUsed,
                       HasWarranty, WarrantyPeriod, IsWarrantyRepair, IsReadyForPickup,
                       ReceiveDate, DeliveryDate, DeviceChecklistJson, AccessoriesJson
                FROM TicketsDB
                ORDER BY ReceiveDate DESC;";

            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapToRepairTicket(reader));
            }

            return list;
        }

        public Task<List<RepairTicket>> GetAllAsync()
            => Task.Run(GetAll);

        public DashboardSummary GetDashboardSummary(DateTime now, DateTime weekStart, DateTime weekEnd)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var overdueCutoff = now.AddDays(-2).ToString("o");
            var weekStartValue = weekStart.ToString("o");
            var weekEndValue = weekEnd.ToString("o");

            const string sql = @"
                SELECT
                    COUNT(*) AS TotalTickets,
                    SUM(CASE WHEN DeliveryDate IS NULL THEN 1 ELSE 0 END) AS OpenTickets,
                    SUM(CASE WHEN DeliveryDate IS NOT NULL THEN 1 ELSE 0 END) AS ClosedTickets,
                    SUM(CASE WHEN DeliveryDate IS NULL AND IFNULL(IsReadyForPickup, 0) = 1 THEN 1 ELSE 0 END) AS ReadyForPickupTickets,
                    SUM(CASE WHEN DeliveryDate IS NULL AND LOWER(IFNULL(PriorityLevel, '')) = 'critical' THEN 1 ELSE 0 END) AS CriticalOpenTickets,
                    SUM(CASE WHEN DeliveryDate IS NULL AND ReceiveDate < @overdueCutoff THEN 1 ELSE 0 END) AS OverdueTickets,
                    SUM(CASE WHEN ReceiveDate >= @weekStart AND ReceiveDate < @weekEnd THEN 1 ELSE 0 END) AS WeeklyTotalTickets,
                    SUM(CASE WHEN DeliveryDate IS NOT NULL AND DeliveryDate >= @weekStart AND DeliveryDate < @weekEnd THEN 1 ELSE 0 END) AS WeeklyFinishedTickets,
                    SUM(CASE WHEN DeliveryDate IS NOT NULL AND DeliveryDate >= @weekStart AND DeliveryDate < @weekEnd THEN IFNULL(FinalCost, 0) ELSE 0 END) AS WeeklyIncome
                FROM TicketsDB;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@overdueCutoff", overdueCutoff);
            cmd.Parameters.AddWithValue("@weekStart", weekStartValue);
            cmd.Parameters.AddWithValue("@weekEnd", weekEndValue);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return new DashboardSummary();
            }

            return new DashboardSummary
            {
                TotalTickets = reader["TotalTickets"] != DBNull.Value ? Convert.ToInt32(reader["TotalTickets"]) : 0,
                OpenTickets = reader["OpenTickets"] != DBNull.Value ? Convert.ToInt32(reader["OpenTickets"]) : 0,
                ClosedTickets = reader["ClosedTickets"] != DBNull.Value ? Convert.ToInt32(reader["ClosedTickets"]) : 0,
                ReadyForPickupTickets = reader["ReadyForPickupTickets"] != DBNull.Value ? Convert.ToInt32(reader["ReadyForPickupTickets"]) : 0,
                CriticalOpenTickets = reader["CriticalOpenTickets"] != DBNull.Value ? Convert.ToInt32(reader["CriticalOpenTickets"]) : 0,
                OverdueTickets = reader["OverdueTickets"] != DBNull.Value ? Convert.ToInt32(reader["OverdueTickets"]) : 0,
                WeeklyTotalTickets = reader["WeeklyTotalTickets"] != DBNull.Value ? Convert.ToInt32(reader["WeeklyTotalTickets"]) : 0,
                WeeklyFinishedTickets = reader["WeeklyFinishedTickets"] != DBNull.Value ? Convert.ToInt32(reader["WeeklyFinishedTickets"]) : 0,
                WeeklyIncome = reader["WeeklyIncome"] != DBNull.Value ? Convert.ToDecimal(reader["WeeklyIncome"]) : 0m
            };
        }

        public Task<DashboardSummary> GetDashboardSummaryAsync(DateTime now, DateTime weekStart, DateTime weekEnd)
            => Task.Run(() => GetDashboardSummary(now, weekStart, weekEnd));

        public List<RepairTicket> GetRecentOpenTickets(int limit)
        {
            var list = new List<RepairTicket>();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = @"
                SELECT TicketId, EscTicketId, CustomerId, CustomerName, PhoneNumber, ContactMethod,
                       DeviceCategory, DeviceBrand, DeviceModel, SerialIMEI, DamageHistory, BoardModifications,
                       ProblemDescription, Notes, RepairStatus, PriorityLevel,
                       EstimatedCost, EstimatedCostCurrency,
                       FinalCost, FinalCostCurrency,
                       RootCause, PartsUsed,
                       HasWarranty, WarrantyPeriod, IsWarrantyRepair, IsReadyForPickup,
                       ReceiveDate, DeliveryDate, DeviceChecklistJson, AccessoriesJson
                FROM TicketsDB
                WHERE DeliveryDate IS NULL
                ORDER BY ReceiveDate DESC
                LIMIT @limit;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@limit", Math.Max(1, limit));
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapToRepairTicket(reader));
            }

            return list;
        }

        public Task<List<RepairTicket>> GetRecentOpenTicketsAsync(int limit)
            => Task.Run(() => GetRecentOpenTickets(limit));

        public Dictionary<string, int> GetOpenPriorityCounts(IEnumerable<string> priorities)
        {
            var normalizedPriorities = priorities
                .Where(priority => !string.IsNullOrWhiteSpace(priority))
                .Select(priority => priority.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var counts = normalizedPriorities.ToDictionary(priority => priority, _ => 0, StringComparer.OrdinalIgnoreCase);
            if (normalizedPriorities.Count == 0)
            {
                return counts;
            }

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var placeholders = normalizedPriorities.Select((_, index) => $"@p{index}").ToArray();
            var sql = $@"
                SELECT PriorityLevel, COUNT(*) AS Count
                FROM TicketsDB
                WHERE DeliveryDate IS NULL
                  AND LOWER(IFNULL(PriorityLevel, '')) IN ({string.Join(",", placeholders.Select(p => $"LOWER({p})"))})
                GROUP BY LOWER(IFNULL(PriorityLevel, ''));";

            using var cmd = new SQLiteCommand(sql, conn);
            for (var i = 0; i < normalizedPriorities.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@p{i}", normalizedPriorities[i]);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var priority = reader["PriorityLevel"]?.ToString() ?? string.Empty;
                var count = reader["Count"] != DBNull.Value ? Convert.ToInt32(reader["Count"]) : 0;

                var key = normalizedPriorities.FirstOrDefault(p => string.Equals(p, priority, StringComparison.OrdinalIgnoreCase));
                if (key != null)
                {
                    counts[key] = count;
                }
            }

            return counts;
        }

        public Task<Dictionary<string, int>> GetOpenPriorityCountsAsync(IEnumerable<string> priorities)
            => Task.Run(() => GetOpenPriorityCounts(priorities));

        public Task<List<RepairTicket>> GetTicketsByClientAsync(string clientName, int? excludeId = null)
            => Task.Run(() =>
            {
                var normalizedClientName = clientName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(normalizedClientName))
                {
                    return new List<RepairTicket>();
                }

                var list = new List<RepairTicket>();

                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                var sql = @"
                    SELECT TicketId, EscTicketId, CustomerId, CustomerName, PhoneNumber, ContactMethod,
                           DeviceCategory, DeviceBrand, DeviceModel, SerialIMEI, DamageHistory, BoardModifications,
                           ProblemDescription, Notes, RepairStatus, PriorityLevel,
                           EstimatedCost, EstimatedCostCurrency,
                           FinalCost, FinalCostCurrency,
                           RootCause, PartsUsed,
                           HasWarranty, WarrantyPeriod, IsWarrantyRepair, IsReadyForPickup,
                           ReceiveDate, DeliveryDate, DeviceChecklistJson, AccessoriesJson
                    FROM TicketsDB
                    WHERE LOWER(IFNULL(TRIM(CustomerName), '')) LIKE @clientNamePattern
                      AND (@excludeId IS NULL OR TicketId <> @excludeId)
                    ORDER BY ReceiveDate DESC
                    LIMIT 10;";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@clientNamePattern", $"%{normalizedClientName.ToLowerInvariant()}%");
                cmd.Parameters.AddWithValue("@excludeId", (object?)excludeId ?? DBNull.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapToRepairTicket(reader));
                }

                return list;
            });
        // ===================== INSERT =====================
        public int Insert(RepairTicket ticket)
        {
            if (string.IsNullOrEmpty(ticket.EscTicketId))
                ticket.EscTicketId = GenerateEscTicketId();

            if (ticket.DeviceChecklistJson == null)
                ticket.DeviceChecklist = new DeviceChecklist();

            if (ticket.AccessoriesJson == null)
                ticket.Accessories = new Accessories();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = @"
                INSERT INTO TicketsDB 
                (EscTicketId, CustomerId, CustomerName, PhoneNumber, ContactMethod,
                 DeviceCategory, DeviceBrand, DeviceModel, SerialIMEI, DamageHistory, BoardModifications,
                 ProblemDescription, Notes, RepairStatus, PriorityLevel,
                 EstimatedCost, EstimatedCostCurrency,
                 FinalCost, FinalCostCurrency,
                 RootCause, PartsUsed,
                 HasWarranty, WarrantyPeriod, IsWarrantyRepair, IsReadyForPickup,
                 ReceiveDate, DeliveryDate, DeviceChecklistJson, AccessoriesJson)
                VALUES
                (@escTicketId, @customerId, @name, @phone, @contact,
                 @cat, @brand, @model, @serial, @damageHistory, @boardMod,
                 @problem, @notes, @status, @priority,
                 @est, @estCur,
                 @final, @finalCur,
                 @rootCause, @parts,
                 @hasWarranty, @warrantyPeriod, @isWarrantyRepair, @ready,
                 @receive, @delivery, @deviceChecklistJson, @accessoriesJson);
                SELECT last_insert_rowid();";

            using var cmd = new SQLiteCommand(sql, conn);
            AddAllParameters(cmd, ticket);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Task<int> InsertAsync(RepairTicket ticket)
            => Task.Run(() => Insert(ticket));

        // ===================== UPDATE =====================
        public void Update(RepairTicket ticket)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var sql = @"
                UPDATE TicketsDB
                SET EscTicketId=@escTicketId, CustomerId=@customerId, CustomerName=@name, PhoneNumber=@phone, 
                    ContactMethod=@contact, DeviceCategory=@cat, DeviceBrand=@brand, DeviceModel=@model, 
                    SerialIMEI=@serial, DamageHistory=@damageHistory, BoardModifications=@boardMod,
                    ProblemDescription=@problem, Notes=@notes, RepairStatus=@status, PriorityLevel=@priority,
                    EstimatedCost=@est, EstimatedCostCurrency=@estCur,
                    FinalCost=@final, FinalCostCurrency=@finalCur,
                    RootCause=@rootCause, PartsUsed=@parts,
                    HasWarranty=@hasWarranty, WarrantyPeriod=@warrantyPeriod, IsWarrantyRepair=@isWarrantyRepair, 
                    IsReadyForPickup=@ready, ReceiveDate=@receive, DeliveryDate=@delivery,
                    DeviceChecklistJson=@deviceChecklistJson, AccessoriesJson=@accessoriesJson,
                    UpdatedAt=CURRENT_TIMESTAMP
                WHERE TicketId=@id;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", ticket.TicketId);
            AddAllParameters(cmd, ticket);

            cmd.ExecuteNonQuery();
        }

        public Task UpdateAsync(RepairTicket ticket)
            => Task.Run(() => Update(ticket));

        // ===================== DELETE =====================
        public void Delete(int ticketId)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            using var cmd = new SQLiteCommand("DELETE FROM TicketsDB WHERE TicketId=@id;", conn);
            cmd.Parameters.AddWithValue("@id", ticketId);
            cmd.ExecuteNonQuery();
        }

        public Task DeleteAsync(int ticketId)
            => Task.Run(() => Delete(ticketId));

        // ===================== MAP =====================
        private RepairTicket MapToRepairTicket(SQLiteDataReader reader)
        {
            return new RepairTicket
            {
                TicketId = Convert.ToInt32(reader["TicketId"]),
                EscTicketId = reader["EscTicketId"]?.ToString(),
                CustomerId = reader["CustomerId"] != DBNull.Value ? Convert.ToInt64(reader["CustomerId"]) : (long?)null,
                CustomerName = reader["CustomerName"]?.ToString(),
                PhoneNumber = reader["PhoneNumber"]?.ToString(),
                ContactMethod = reader["ContactMethod"]?.ToString(),

                DeviceCategory = reader["DeviceCategory"]?.ToString(),
                DeviceBrand = reader["DeviceBrand"]?.ToString(),
                DeviceModel = reader["DeviceModel"]?.ToString(),
                SerialIMEI = reader["SerialIMEI"]?.ToString(),
                DamageHistory = reader["DamageHistory"]?.ToString(),
                BoardModifications = reader["BoardModifications"]?.ToString(),

                ProblemDescription = reader["ProblemDescription"]?.ToString(),
                Notes = reader["Notes"]?.ToString(),

                RepairStatus = reader["RepairStatus"]?.ToString(),
                PriorityLevel = reader["PriorityLevel"]?.ToString(),

                EstimatedCost = reader["EstimatedCost"] != DBNull.Value ? Convert.ToDecimal(reader["EstimatedCost"]) : (decimal?)null,
                EstimatedCostCurrency = reader["EstimatedCostCurrency"]?.ToString() ?? "S.P",

                FinalCost = reader["FinalCost"] != DBNull.Value ? Convert.ToDecimal(reader["FinalCost"]) : (decimal?)null,
                FinalCostCurrency = reader["FinalCostCurrency"]?.ToString() ?? "S.P",

                RootCause = reader["RootCause"]?.ToString(),
                PartsUsed = reader["PartsUsed"]?.ToString(),

                HasWarranty = reader["HasWarranty"] != DBNull.Value && Convert.ToBoolean(reader["HasWarranty"]),
                WarrantyPeriod = reader["WarrantyPeriod"]?.ToString(),
                IsWarrantyRepair = reader["IsWarrantyRepair"] != DBNull.Value && Convert.ToBoolean(reader["IsWarrantyRepair"]),
                IsReadyForPickup = reader["IsReadyForPickup"] != DBNull.Value && Convert.ToBoolean(reader["IsReadyForPickup"]),

                ReceiveDate = DateTime.Parse(reader["ReceiveDate"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
                DeliveryDate = reader["DeliveryDate"] != DBNull.Value
                    ? DateTime.Parse(reader["DeliveryDate"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                    : (DateTime?)null,

                DeviceChecklistJson = reader["DeviceChecklistJson"]?.ToString() ?? "{}",
                AccessoriesJson = reader["AccessoriesJson"]?.ToString() ?? "{}"
            };
        }

        // ===================== PARAMETERS =====================
        private void AddAllParameters(SQLiteCommand cmd, RepairTicket ticket)
        {
            cmd.Parameters.AddWithValue("@escTicketId", ticket.EscTicketId ?? GenerateEscTicketId());
            cmd.Parameters.AddWithValue("@customerId", ticket.CustomerId.HasValue ? (object)ticket.CustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@name", ticket.CustomerName ?? string.Empty);
            cmd.Parameters.AddWithValue("@phone", ticket.PhoneNumber ?? string.Empty);
            cmd.Parameters.AddWithValue("@contact", ticket.ContactMethod ?? string.Empty);

            cmd.Parameters.AddWithValue("@cat", ticket.DeviceCategory ?? string.Empty);
            cmd.Parameters.AddWithValue("@brand", ticket.DeviceBrand ?? string.Empty);
            cmd.Parameters.AddWithValue("@model", ticket.DeviceModel ?? string.Empty);
            cmd.Parameters.AddWithValue("@serial", ticket.SerialIMEI ?? string.Empty);
            cmd.Parameters.AddWithValue("@damageHistory", ticket.DamageHistory ?? string.Empty);
            cmd.Parameters.AddWithValue("@boardMod", ticket.BoardModifications ?? string.Empty);

            cmd.Parameters.AddWithValue("@problem", ticket.ProblemDescription ?? string.Empty);
            cmd.Parameters.AddWithValue("@notes", ticket.Notes ?? string.Empty);

            cmd.Parameters.AddWithValue("@status", ticket.RepairStatus ?? "Received");
            cmd.Parameters.AddWithValue("@priority", ticket.PriorityLevel ?? "Normal");

            cmd.Parameters.AddWithValue("@est", ticket.EstimatedCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@estCur", ticket.EstimatedCostCurrency ?? "S.P");

            cmd.Parameters.AddWithValue("@final", ticket.FinalCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@finalCur", ticket.FinalCostCurrency ?? "S.P");

            cmd.Parameters.AddWithValue("@rootCause", ticket.RootCause ?? string.Empty);
            cmd.Parameters.AddWithValue("@parts", ticket.PartsUsed ?? string.Empty);

            cmd.Parameters.AddWithValue("@hasWarranty", ticket.HasWarranty);
            cmd.Parameters.AddWithValue("@warrantyPeriod", ticket.WarrantyPeriod ?? string.Empty);
            cmd.Parameters.AddWithValue("@isWarrantyRepair", ticket.IsWarrantyRepair);
            cmd.Parameters.AddWithValue("@ready", ticket.IsReadyForPickup);

            cmd.Parameters.AddWithValue("@receive", ticket.ReceiveDate.ToString("o"));
            cmd.Parameters.AddWithValue("@delivery", ticket.DeliveryDate?.ToString("o") ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@deviceChecklistJson", ticket.DeviceChecklistJson ?? "{}");
            cmd.Parameters.AddWithValue("@accessoriesJson", ticket.AccessoriesJson ?? "{}");
        }

        // ===================== ESC ID =====================
        private string GenerateEscTicketId()
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                WITH RECURSIVE seq(n) AS (
                    SELECT 1
                    UNION ALL
                    SELECT n + 1
                    FROM seq
                    WHERE n < (
                        SELECT COALESCE(MAX(CAST(SUBSTR(EscTicketId, 5) AS INTEGER)), 0) + 1
                        FROM TicketsDB
                        WHERE EscTicketId GLOB 'ESC-[0-9][0-9][0-9][0-9][0-9][0-9]'
                    )
                )
                SELECT n
                FROM seq
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM TicketsDB t
                    WHERE t.EscTicketId = printf('ESC-%06d', seq.n)
                )
                ORDER BY n
                LIMIT 1;";

            using var cmd = new SQLiteCommand(sql, conn);
            var result = cmd.ExecuteScalar();

            var nextNumber = result != null && result != DBNull.Value
                ? Convert.ToInt32(result)
                : 1;

            return $"ESC-{nextNumber:D6}";
        }


        public string GetNextEscTicketId() => GenerateEscTicketId();
        public Task<string> GetNextEscTicketIdAsync() => Task.Run(GenerateEscTicketId);

        public sealed class DashboardSummary
        {
            public int TotalTickets { get; set; }
            public int OpenTickets { get; set; }
            public int ClosedTickets { get; set; }
            public int ReadyForPickupTickets { get; set; }
            public int CriticalOpenTickets { get; set; }
            public int OverdueTickets { get; set; }
            public int WeeklyTotalTickets { get; set; }
            public int WeeklyFinishedTickets { get; set; }
            public decimal WeeklyIncome { get; set; }
        }
    }
}
