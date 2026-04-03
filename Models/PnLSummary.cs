namespace ESCenter.Models;

public class PnLSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalPartsCost { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMarginPercent { get; set; }
}
