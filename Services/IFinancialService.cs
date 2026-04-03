using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESCenter.Models;

namespace ESCenter.Services;

public interface IFinancialService
{
    Task<decimal> GetRevenueAsync(DateTime from, DateTime to);
    Task<decimal> GetPartsCostAsync(DateTime from, DateTime to);
    Task<List<ExpenseRecord>> GetExpensesAsync(DateTime from, DateTime to);
    Task<List<TicketProfit>> GetProfitPerTicketAsync(DateTime from, DateTime to);
    Task<List<MonthlyBreakdown>> GetMonthlyBreakdownAsync(int year);
    Task<List<BrandRevenue>> GetRevenueByBrandAsync(DateTime from, DateTime to);
    Task<WarrantyCostSummary> GetWarrantyCostAsync(DateTime from, DateTime to);
}
