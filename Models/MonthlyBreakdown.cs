namespace ESCenter.Models;

public class MonthlyBreakdown
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
}
