using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
                       ProblemDescription, Notes, RepairStatus, PriorityLevel, TicketType,
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
                 ProblemDescription, Notes, RepairStatus, PriorityLevel, TicketType,
                 EstimatedCost, EstimatedCostCurrency,
                 FinalCost, FinalCostCurrency,
                 RootCause, PartsUsed,
                 HasWarranty, WarrantyPeriod, IsWarrantyRepair, IsReadyForPickup,
                 ReceiveDate, DeliveryDate, DeviceChecklistJson, AccessoriesJson)
                VALUES
                (@escTicketId, @customerId, @name, @phone, @contact,
                 @cat, @brand, @model, @serial, @damageHistory, @boardMod,
                 @problem, @notes, @status, @priority, @ticketType,
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
                    ProblemDescription=@problem, Notes=@notes, RepairStatus=@status, PriorityLevel=@priority, TicketType=@ticketType,
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
                TicketType = reader["TicketType"]?.ToString() ?? "Normal",

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
            cmd.Parameters.AddWithValue("@ticketType", ticket.TicketType ?? "Normal");

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

            using var cmd = new SQLiteCommand("SELECT EscTicketId FROM TicketsDB WHERE EscTicketId IS NOT NULL AND TRIM(EscTicketId) <> '';", conn);
            using var reader = cmd.ExecuteReader();

            var allocatedNumbers = new HashSet<int>();

            while (reader.Read())
            {
                var escId = reader["EscTicketId"]?.ToString();
                if (TryParseEscIdNumber(escId, out var parsedNumber) && parsedNumber > 0)
                {
                    allocatedNumbers.Add(parsedNumber);
                }
            }

            var nextNumber = 1;
            while (allocatedNumbers.Contains(nextNumber))
            {
                nextNumber++;
            }

            return $"ESC-{nextNumber:D6}";
        }

        private static bool TryParseEscIdNumber(string escId, out int number)
        {
            number = 0;

            if (string.IsNullOrWhiteSpace(escId))
            {
                return false;
            }

            var normalized = escId.Trim();
            const string prefix = "ESC-";
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return int.TryParse(normalized.Substring(prefix.Length), out number);
        }

        public string GetNextEscTicketId() => GenerateEscTicketId();
        public Task<string> GetNextEscTicketIdAsync() => Task.Run(GenerateEscTicketId);
    }
}
