using System;

namespace ESCenter.Models
{
    public class TicketStatusEntry
    {
        public int HistoryId { get; set; }
        public int TicketId { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string Note { get; set; }
        public DateTime ChangedAt { get; set; }

        public string ChangedAtDisplay => ChangedAt.ToString("yyyy-MM-dd HH:mm");
    }
}
