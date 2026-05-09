using System;

namespace ESCenter.Services
{
    public static class TicketEvents
    {
        public static event EventHandler? TicketsChanged;
        public static event EventHandler? StockChanged;

        public static void RaiseTicketsChanged()
        {
            TicketsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void RaiseStockChanged()
        {
            StockChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
