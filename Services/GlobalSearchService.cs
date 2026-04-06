using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using ESCenter.Models;

namespace ESCenter.Services
{
    public class GlobalSearchService
    {
        private string ConnectionString =>
            $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        /// <summary>
        /// Search all tables for items matching the query string.
        /// Returns up to 12 results ordered by relevance.
        /// Never throws — returns empty list on any error.
        /// </summary>
        public List<GlobalSearchResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return new List<GlobalSearchResult>();
            }

            var q = query.Trim();
            var results = new List<GlobalSearchResult>();

            try
            {
                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                results.AddRange(SearchTickets(conn, q));
                results.AddRange(SearchParts(conn, q));
                results.AddRange(SearchInventory(conn, q));
                results.AddRange(SearchBoneyard(conn, q));
            }
            catch
            {
                // best-effort only
            }

            // Rank: exact identifier match > starts with > contains
            return results
                .OrderByDescending(r =>
                    r.Identifier.Equals(q, StringComparison.OrdinalIgnoreCase) ? 2 :
                    r.Identifier.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(r => r.Source)
                .ThenBy(r => r.Identifier)
                .Take(12)
                .ToList();
        }

        private List<GlobalSearchResult> SearchTickets(SQLiteConnection conn, string q)
        {
            var results = new List<GlobalSearchResult>();
            using var cmd = new SQLiteCommand(@"
                SELECT TicketId, EscTicketId, CustomerName, PhoneNumber,
                       DeviceBrand, DeviceModel, SerialIMEI
                FROM TicketsDB
                WHERE EscTicketId LIKE @q
                   OR CustomerName LIKE @q
                   OR PhoneNumber LIKE @q
                   OR DeviceModel LIKE @q
                   OR SerialIMEI LIKE @q
                LIMIT 5;", conn);
            cmd.Parameters.AddWithValue("@q", $"%{q}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var escId = reader["EscTicketId"]?.ToString() ?? string.Empty;
                var customer = reader["CustomerName"]?.ToString() ?? string.Empty;
                var brand = reader["DeviceBrand"]?.ToString() ?? string.Empty;
                var model = reader["DeviceModel"]?.ToString() ?? string.Empty;
                var device = string.Join(" ", new[] { brand, model }.Where(s => !string.IsNullOrWhiteSpace(s)));

                results.Add(new GlobalSearchResult
                {
                    Identifier = escId,
                    Name = string.IsNullOrWhiteSpace(customer) ? device : $"{customer} — {device}",
                    Source = "Repair Tickets",
                    NavigationTarget = "Tickets",
                    ItemId = reader["TicketId"]?.ToString() ?? string.Empty
                });
            }

            return results;
        }

        private List<GlobalSearchResult> SearchParts(SQLiteConnection conn, string q)
        {
            var results = new List<GlobalSearchResult>();
            using var cmd = new SQLiteCommand(@"
                SELECT PartId, SKU, PartCode, PartType, Description
                FROM Parts
                WHERE SKU LIKE @q
                   OR PartCode LIKE @q
                   OR PartType LIKE @q
                   OR Description LIKE @q
                LIMIT 4;", conn);
            cmd.Parameters.AddWithValue("@q", $"%{q}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var sku = reader["SKU"]?.ToString() ?? string.Empty;
                var code = reader["PartCode"]?.ToString() ?? string.Empty;
                var identifier = !string.IsNullOrWhiteSpace(sku) ? sku : code;
                var name = reader["PartType"]?.ToString() ?? string.Empty;

                results.Add(new GlobalSearchResult
                {
                    Identifier = identifier,
                    Name = name,
                    Source = "Parts Control",
                    NavigationTarget = "Parts",
                    ItemId = reader["PartId"]?.ToString() ?? string.Empty
                });
            }

            return results;
        }

        private List<GlobalSearchResult> SearchInventory(SQLiteConnection conn, string q)
        {
            var results = new List<GlobalSearchResult>();
            using var cmd = new SQLiteCommand(@"
                SELECT InventoryId, ItemType, Brand, Model, Description
                FROM Inventory
                WHERE ItemType LIKE @q
                   OR Brand LIKE @q
                   OR Model LIKE @q
                   OR Description LIKE @q
                LIMIT 4;", conn);
            cmd.Parameters.AddWithValue("@q", $"%{q}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var itemType = reader["ItemType"]?.ToString() ?? string.Empty;
                var brand = reader["Brand"]?.ToString() ?? string.Empty;
                var model = reader["Model"]?.ToString() ?? string.Empty;
                var description = reader["Description"]?.ToString() ?? string.Empty;
                var displayName = !string.IsNullOrWhiteSpace(description)
                    ? description
                    : string.Join(" ", new[] { itemType, brand, model }.Where(s => !string.IsNullOrWhiteSpace(s)));

                results.Add(new GlobalSearchResult
                {
                    Identifier = itemType,
                    Name = displayName,
                    Source = "Inventory",
                    NavigationTarget = "Inventory",
                    ItemId = reader["InventoryId"]?.ToString() ?? string.Empty
                });
            }

            return results;
        }

        private List<GlobalSearchResult> SearchBoneyard(SQLiteConnection conn, string q)
        {
            var results = new List<GlobalSearchResult>();
            using var cmd = new SQLiteCommand(@"
                SELECT DeviceId, Brand, Model, HolderID, Notes
                FROM Boneyard
                WHERE Brand LIKE @q
                   OR Model LIKE @q
                   OR HolderID LIKE @q
                LIMIT 3;", conn);
            cmd.Parameters.AddWithValue("@q", $"%{q}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var brand = reader["Brand"]?.ToString() ?? string.Empty;
                var model = reader["Model"]?.ToString() ?? string.Empty;
                var identifier = $"BY-{reader["DeviceId"]}";

                results.Add(new GlobalSearchResult
                {
                    Identifier = identifier,
                    Name = $"{brand} {model}".Trim(),
                    Source = "Boneyard",
                    NavigationTarget = "Boneyard",
                    ItemId = reader["DeviceId"]?.ToString() ?? string.Empty
                });
            }

            return results;
        }
    }
}
