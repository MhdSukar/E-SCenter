using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ESCenter.Models;

namespace ESCenter.Services;

public class FinancialService : IFinancialService
{
    private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

    public async Task<decimal> GetRevenueAsync(DateTime from, DateTime to)
    {
        return await Task.Run(() =>
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand(@"
                SELECT COALESCE(SUM(FinalCost),0)
                FROM TicketsDB
                WHERE DeliveryDate IS NOT NULL
                  AND date(DeliveryDate) BETWEEN date(@from) AND date(@to);", conn);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
            return Convert.ToDecimal(cmd.ExecuteScalar());
        });
    }

    public async Task<decimal> GetPartsCostAsync(DateTime from, DateTime to)
    {
        var profits = await GetProfitPerTicketAsync(from, to);
        return profits.Sum(x => x.PartsCost);
    }

    public async Task<List<ExpenseRecord>> GetExpensesAsync(DateTime from, DateTime to)
    {
        return await Task.Run(() =>
        {
            var records = new List<ExpenseRecord>();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                SELECT AlBarakaId, Date, Category, COALESCE(Amount, Price, 0) Amount
                FROM AlBaraka
                WHERE date(Date) BETWEEN date(@from) AND date(@to)
                ORDER BY date(Date) DESC;", conn);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new ExpenseRecord
                {
                    ExpenseId = Convert.ToInt32(reader["AlBarakaId"]),
                    Date = ParseDate(reader["Date"]?.ToString()),
                    Category = reader["Category"]?.ToString() ?? "General",
                    Amount = Convert.ToDecimal(reader["Amount"])
                });
            }

            return records;
        });
    }

    public async Task<List<TicketProfit>> GetProfitPerTicketAsync(DateTime from, DateTime to)
    {
        return await Task.Run(() =>
        {
            var tickets = new List<RepairTicket>();
            var inventoryPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            using (var inventoryCmd = new SQLiteCommand("SELECT Description, ItemType, Brand, Model, Price FROM Inventory;", conn))
            using (var inventoryReader = inventoryCmd.ExecuteReader())
            {
                while (inventoryReader.Read())
                {
                    var price = inventoryReader["Price"] != DBNull.Value ? Convert.ToDecimal(inventoryReader["Price"]) : 0m;
                    var description = inventoryReader["Description"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(description)) inventoryPrices[description.Trim()] = price;

                    var fallback = string.Join(" ", new[]
                    {
                        inventoryReader["ItemType"]?.ToString(),
                        inventoryReader["Brand"]?.ToString(),
                        inventoryReader["Model"]?.ToString()
                    }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
                    if (!string.IsNullOrWhiteSpace(fallback) && !inventoryPrices.ContainsKey(fallback))
                    {
                        inventoryPrices[fallback] = price;
                    }
                }
            }

            using (var ticketCmd = new SQLiteCommand(@"
                SELECT TicketId, EscTicketId, DeviceCategory, DeviceBrand, DeviceModel,
                       ProblemDescription, FinalCost, EstimatedCost, PartsUsed, DeliveryDate
                FROM TicketsDB
                WHERE DeliveryDate IS NOT NULL
                  AND date(DeliveryDate) BETWEEN date(@from) AND date(@to)
                ORDER BY date(DeliveryDate) DESC;", conn))
            {
                ticketCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                ticketCmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
                using var reader = ticketCmd.ExecuteReader();
                while (reader.Read())
                {
                    tickets.Add(new RepairTicket
                    {
                        TicketId = Convert.ToInt32(reader["TicketId"]),
                        EscTicketId = reader["EscTicketId"]?.ToString() ?? string.Empty,
                        DeviceCategory = reader["DeviceCategory"]?.ToString() ?? string.Empty,
                        DeviceBrand = reader["DeviceBrand"]?.ToString() ?? string.Empty,
                        DeviceModel = reader["DeviceModel"]?.ToString() ?? string.Empty,
                        ProblemDescription = reader["ProblemDescription"]?.ToString() ?? string.Empty,
                        FinalCost = reader["FinalCost"] != DBNull.Value ? Convert.ToDecimal(reader["FinalCost"]) : 0m,
                        EstimatedCost = reader["EstimatedCost"] != DBNull.Value ? Convert.ToDecimal(reader["EstimatedCost"]) : 0m,
                        PartsUsed = reader["PartsUsed"]?.ToString() ?? string.Empty,
                        DeliveryDate = reader["DeliveryDate"] != DBNull.Value ? DateTime.Parse(reader["DeliveryDate"].ToString() ?? string.Empty) : null
                    });
                }
            }

            return tickets.Select(ticket =>
            {
                var partsCost = EstimatePartsCost(ticket.PartsUsed, inventoryPrices);
                var laborCost = ticket.EstimatedCost ?? 0m;
                var revenue = ticket.FinalCost ?? 0m;
                var net = revenue - partsCost - laborCost;
                var margin = revenue <= 0 ? 0 : (net / revenue) * 100m;
                return new TicketProfit
                {
                    TicketId = ticket.TicketId,
                    EscTicketId = string.IsNullOrWhiteSpace(ticket.EscTicketId) ? $"ESC-{ticket.TicketId:D6}" : ticket.EscTicketId,
                    Device = string.Join(" ", new[] { ticket.DeviceCategory, ticket.DeviceBrand, ticket.DeviceModel }
                        .Where(x => !string.IsNullOrWhiteSpace(x))),
                    Problem = ticket.ProblemDescription,
                    Revenue = revenue,
                    PartsCost = partsCost,
                    LaborCost = laborCost,
                    NetProfit = net,
                    MarginPercent = margin,
                    ClosedDate = ticket.DeliveryDate
                };
            }).ToList();
        });
    }

    public async Task<List<MonthlyBreakdown>> GetMonthlyBreakdownAsync(int year)
    {
        return await Task.Run(() =>
        {
            var from = new DateTime(year, 1, 1);
            var to = new DateTime(year, 12, 31);
            var revenueByMonth = new Dictionary<int, decimal>();
            var expenseByMonth = new Dictionary<int, decimal>();

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            using (var cmd = new SQLiteCommand(@"
                SELECT CAST(strftime('%m', DeliveryDate) AS INTEGER) M, COALESCE(SUM(FinalCost),0) Total
                FROM TicketsDB
                WHERE DeliveryDate IS NOT NULL
                  AND date(DeliveryDate) BETWEEN date(@from) AND date(@to)
                GROUP BY strftime('%m', DeliveryDate);", conn))
            {
                cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    revenueByMonth[Convert.ToInt32(reader["M"])] = Convert.ToDecimal(reader["Total"]);
                }
            }

            using (var cmd = new SQLiteCommand(@"
                SELECT CAST(strftime('%m', Date) AS INTEGER) M, COALESCE(SUM(COALESCE(Amount, Price, 0)),0) Total
                FROM AlBaraka
                WHERE date(Date) BETWEEN date(@from) AND date(@to)
                GROUP BY strftime('%m', Date);", conn))
            {
                cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    expenseByMonth[Convert.ToInt32(reader["M"])] = Convert.ToDecimal(reader["Total"]);
                }
            }

            return Enumerable.Range(1, 12)
                .Select(month => new MonthlyBreakdown
                {
                    MonthLabel = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[month - 1],
                    Revenue = revenueByMonth.GetValueOrDefault(month),
                    Expenses = expenseByMonth.GetValueOrDefault(month)
                })
                .ToList();
        });
    }

    public async Task<List<BrandRevenue>> GetRevenueByBrandAsync(DateTime from, DateTime to)
    {
        return await Task.Run(() =>
        {
            var list = new List<BrandRevenue>();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand(@"
                SELECT COALESCE(NULLIF(TRIM(DeviceBrand), ''), 'Unknown') Brand, COALESCE(SUM(FinalCost),0) Revenue
                FROM TicketsDB
                WHERE DeliveryDate IS NOT NULL
                  AND date(DeliveryDate) BETWEEN date(@from) AND date(@to)
                GROUP BY COALESCE(NULLIF(TRIM(DeviceBrand), ''), 'Unknown')
                ORDER BY Revenue DESC;", conn);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BrandRevenue
                {
                    Brand = reader["Brand"]?.ToString() ?? "Unknown",
                    Revenue = Convert.ToDecimal(reader["Revenue"])
                });
            }

            return list;
        });
    }

    public async Task<WarrantyCostSummary> GetWarrantyCostAsync(DateTime from, DateTime to)
    {
        return await Task.Run(() =>
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand(@"
                SELECT COUNT(*) RepairCount, COALESCE(SUM(COALESCE(FinalCost, EstimatedCost, 0)),0) RevenueLost
                FROM TicketsDB
                WHERE IsWarrantyRepair = 1
                  AND date(ReceiveDate) BETWEEN date(@from) AND date(@to);", conn);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return new WarrantyCostSummary();
            return new WarrantyCostSummary
            {
                RepairCount = Convert.ToInt32(reader["RepairCount"]),
                RevenueLost = Convert.ToDecimal(reader["RevenueLost"])
            };
        });
    }

    private static decimal EstimatePartsCost(string? partsJson, Dictionary<string, decimal> priceLookup)
    {
        if (string.IsNullOrWhiteSpace(partsJson)) return 0m;
        try
        {
            var parts = JsonSerializer.Deserialize<List<string>>(partsJson) ?? new List<string>();
            decimal total = 0;
            foreach (var part in parts.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var key = part.Trim();
                if (priceLookup.TryGetValue(key, out var unitPrice)) total += unitPrice;
            }

            return total;
        }
        catch
        {
            return 0m;
        }
    }

    private static DateTime ParseDate(string? dateText)
    {
        if (DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return DateTime.TryParse(dateText, out var fallback) ? fallback : DateTime.Today;
    }
}
