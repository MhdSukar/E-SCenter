using System;

namespace ESCenter.Services
{
    public static class TicketEvents
    {
        public static event EventHandler? TicketsChanged;

        public static void RaiseTicketsChanged()
        {
            TicketsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
