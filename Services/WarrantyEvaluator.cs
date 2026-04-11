using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ESCenter.Models;

namespace ESCenter.Services
{
    internal static class WarrantyEvaluator
    {
        public static bool HasActiveWarranty(RepairTicket ticket)
        {
            return TryGetExpiration(ticket, out var expiration) && expiration.Date >= DateTime.Today;
        }

        public static bool TryGetExpiration(RepairTicket ticket, out DateTime expiration)
        {
            expiration = GetWarrantyStart(ticket);

            if (ticket == null || !ticket.HasWarranty || string.IsNullOrWhiteSpace(ticket.WarrantyPeriod))
            {
                return false;
            }

            var normalized = ticket.WarrantyPeriod.Trim().ToLowerInvariant();
            var digits = new string(normalized.Where(char.IsDigit).ToArray());

            if (!int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            {
                return false;
            }

            if (normalized.Contains("year"))
            {
                expiration = expiration.AddYears(amount);
                return true;
            }

            if (normalized.Contains("month"))
            {
                expiration = expiration.AddMonths(amount);
                return true;
            }

            if (normalized.Contains("day") || normalized.Contains("hour"))
            {
                expiration = expiration.AddDays(amount);
                return true;
            }

            expiration = expiration.AddDays(amount);
            return true;
        }

        public static List<RepairTicket> GetExpiringWarranties(IEnumerable<RepairTicket> tickets, int withinDays)
        {
            if (tickets == null || withinDays < 0)
            {
                return new List<RepairTicket>();
            }

            var today = DateTime.Today;
            var maxDate = today.AddDays(withinDays);

            return tickets
                .Where(t => t != null
                            && !t.DeliveryDate.HasValue
                            && !string.Equals(t.RepairStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
                            && TryGetExpiration(t, out var expiration)
                            && expiration.Date >= today
                            && expiration.Date <= maxDate)
                .ToList();
        }

        private static DateTime GetWarrantyStart(RepairTicket ticket)
        {
            return ticket.DeliveryDate?.Date ?? ticket.ReceiveDate.Date;
        }
    }
}
