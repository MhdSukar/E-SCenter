namespace ESCenter.Models;

public class ExpenseCategory
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ExpensePercent { get; set; }
    public int TransactionCount { get; set; }
}
