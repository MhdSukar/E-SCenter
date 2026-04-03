using System;

namespace ESCenter.Models;

public class ExpenseRecord
{
    public int ExpenseId { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
