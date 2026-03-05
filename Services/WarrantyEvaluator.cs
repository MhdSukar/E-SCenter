using System;
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

        private static DateTime GetWarrantyStart(RepairTicket ticket)
        {
            return ticket.DeliveryDate?.Date ?? ticket.ReceiveDate.Date;
        }
    }
}
