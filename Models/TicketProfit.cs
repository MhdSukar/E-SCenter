using System;

namespace ESCenter.Models;

public class TicketProfit
{
    public int TicketId { get; set; }
    public string EscTicketId { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public DateTime? ClosedDate { get; set; }
}
