using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESCenter.Models;
using ESCenter.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ESCenter.ViewModels;

public partial class PnLDashboardViewModel : ObservableObject
{
    private readonly IFinancialService _financialService;

    public ObservableCollection<string> PeriodOptions { get; } = new(new[] { "Today", "This Week", "This Month", "This Year", "Custom Range" });
    public ObservableCollection<TicketProfit> ProfitPerTicket { get; } = new();
    public ObservableCollection<ExpenseCategory> ExpenseBreakdown { get; } = new();

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string selectedPeriod = "This Month";
    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;

    [ObservableProperty] private decimal totalRevenue;
    [ObservableProperty] private decimal totalPartsCost;
    [ObservableProperty] private decimal totalExpenses;
    [ObservableProperty] private decimal grossProfit;
    [ObservableProperty] private decimal netProfit;
    [ObservableProperty] private decimal profitMarginPercent;

    [ObservableProperty] private decimal revenueTrendPercent;
    [ObservableProperty] private decimal partsTrendPercent;
    [ObservableProperty] private decimal expensesTrendPercent;
    [ObservableProperty] private decimal grossTrendPercent;
    [ObservableProperty] private decimal netTrendPercent;
    [ObservableProperty] private decimal marginTrendPercent;

    [ObservableProperty] private int warrantyRepairCount;
    [ObservableProperty] private decimal warrantyRevenueLost;

    [ObservableProperty] private IEnumerable<ISeries> monthlyRevenueExpensesSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] monthlyAxisX = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] monthlyAxisY = Array.Empty<Axis>();

    [ObservableProperty] private IEnumerable<ISeries> netProfitTrendSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] netProfitAxisX = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] netProfitAxisY = Array.Empty<Axis>();

    [ObservableProperty] private IEnumerable<ISeries> revenueBreakdownSeries = Array.Empty<ISeries>();
    [ObservableProperty] private IEnumerable<ISeries> topRepairsSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] topRepairsAxisX = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] topRepairsAxisY = Array.Empty<Axis>();

    public PnLDashboardViewModel() : this(new FinancialService())
    {
    }

    public PnLDashboardViewModel(IFinancialService financialService)
    {
        _financialService = financialService;
        ApplyPeriodBounds();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var revenue = await _financialService.GetRevenueAsync(FromDate, ToDate);
            var partsCost = await _financialService.GetPartsCostAsync(FromDate, ToDate);
            var expenses = await _financialService.GetExpensesAsync(FromDate, ToDate);
            var expensesTotal = expenses.Sum(e => e.Amount);
            var ticketProfits = await _financialService.GetProfitPerTicketAsync(FromDate, ToDate);
            var brandRevenue = await _financialService.GetRevenueByBrandAsync(FromDate, ToDate);
            var monthly = await _financialService.GetMonthlyBreakdownAsync(FromDate.Year);
            var warranty = await _financialService.GetWarrantyCostAsync(FromDate, ToDate);

            TotalRevenue = revenue;
            TotalPartsCost = partsCost;
            TotalExpenses = expensesTotal;
            GrossProfit = revenue - partsCost;
            NetProfit = GrossProfit - expensesTotal;
            ProfitMarginPercent = revenue <= 0 ? 0 : (NetProfit / revenue) * 100m;

            WarrantyRepairCount = warranty.RepairCount;
            WarrantyRevenueLost = warranty.RevenueLost;

            BuildProfitTable(ticketProfits);
            BuildExpenseBreakdown(expenses);
            BuildMonthlyChart(monthly);
            BuildNetProfitTrend(ticketProfits);
            BuildRevenueBreakdownChart(brandRevenue);
            BuildTopRepairsChart(ticketProfits);

            await ComputeTrendsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PeriodChangedAsync()
    {
        ApplyPeriodBounds();
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ExportToPdf()
    {
    }

    [RelayCommand]
    private void ExportToExcel()
    {
    }

    partial void OnFromDateChanged(DateTime value)
    {
        if (SelectedPeriod == "Custom Range") _ = LoadDataAsync();
    }

    partial void OnToDateChanged(DateTime value)
    {
        if (SelectedPeriod == "Custom Range") _ = LoadDataAsync();
    }

    private void ApplyPeriodBounds()
    {
        var today = DateTime.Today;
        switch (SelectedPeriod)
        {
            case "Today":
                FromDate = today;
                ToDate = today;
                break;
            case "This Week":
                var start = today.AddDays(-(int)today.DayOfWeek);
                FromDate = start;
                ToDate = start.AddDays(6);
                break;
            case "This Month":
                FromDate = new DateTime(today.Year, today.Month, 1);
                ToDate = FromDate.AddMonths(1).AddDays(-1);
                break;
            case "This Year":
                FromDate = new DateTime(today.Year, 1, 1);
                ToDate = new DateTime(today.Year, 12, 31);
                break;
        }
    }

    private async Task ComputeTrendsAsync()
    {
        var span = (ToDate.Date - FromDate.Date).Days + 1;
        var previousTo = FromDate.AddDays(-1);
        var previousFrom = previousTo.AddDays(-Math.Max(1, span) + 1);

        var prevRevenue = await _financialService.GetRevenueAsync(previousFrom, previousTo);
        var prevParts = await _financialService.GetPartsCostAsync(previousFrom, previousTo);
        var prevExpense = (await _financialService.GetExpensesAsync(previousFrom, previousTo)).Sum(x => x.Amount);

        var prevGross = prevRevenue - prevParts;
        var prevNet = prevGross - prevExpense;
        var prevMargin = prevRevenue <= 0 ? 0 : (prevNet / prevRevenue) * 100m;

        RevenueTrendPercent = TrendPercent(TotalRevenue, prevRevenue);
        PartsTrendPercent = TrendPercent(TotalPartsCost, prevParts);
        ExpensesTrendPercent = TrendPercent(TotalExpenses, prevExpense);
        GrossTrendPercent = TrendPercent(GrossProfit, prevGross);
        NetTrendPercent = TrendPercent(NetProfit, prevNet);
        MarginTrendPercent = TrendPercent(ProfitMarginPercent, prevMargin);
    }

    private static decimal TrendPercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return ((current - previous) / Math.Abs(previous)) * 100m;
    }

    private void BuildProfitTable(IEnumerable<TicketProfit> rows)
    {
        ProfitPerTicket.Clear();
        foreach (var row in rows.OrderByDescending(r => r.ClosedDate))
        {
            ProfitPerTicket.Add(row);
        }
    }

    private void BuildExpenseBreakdown(IEnumerable<ExpenseRecord> expenses)
    {
        ExpenseBreakdown.Clear();
        var total = expenses.Sum(x => x.Amount);
        var grouped = expenses.GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "General" : x.Category)
            .Select(g => new ExpenseCategory
            {
                Category = g.Key,
                TotalAmount = g.Sum(x => x.Amount),
                TransactionCount = g.Count(),
                ExpensePercent = total == 0 ? 0 : (g.Sum(x => x.Amount) / total) * 100m
            }).OrderByDescending(x => x.TotalAmount);

        foreach (var group in grouped)
        {
            ExpenseBreakdown.Add(group);
        }
    }

    private void BuildMonthlyChart(IReadOnlyCollection<MonthlyBreakdown> monthly)
    {
        MonthlyRevenueExpensesSeries = new ISeries[]
        {
            new ColumnSeries<decimal> { Name = "Revenue", Values = monthly.Select(x => x.Revenue).ToArray(), Fill = new SolidColorPaint(SKColor.Parse("#4E84C4")) },
            new ColumnSeries<decimal> { Name = "Expenses", Values = monthly.Select(x => x.Expenses).ToArray(), Fill = new SolidColorPaint(SKColor.Parse("#DC3737")) }
        };
        MonthlyAxisX = new[] { new Axis { Labels = monthly.Select(x => x.MonthLabel).ToArray() } };
        MonthlyAxisY = new[] { new Axis { Labeler = value => value.ToString("N0") } };
    }

    private void BuildNetProfitTrend(IReadOnlyCollection<TicketProfit> ticketProfits)
    {
        var ordered = ticketProfits.OrderBy(t => t.ClosedDate).ToList();
        NetProfitTrendSeries = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Name = "Net Profit",
                Values = ordered.Select(x => x.NetProfit).ToArray(),
                Stroke = new SolidColorPaint(NetProfit >= 0 ? SKColor.Parse("#2DC4B0") : SKColor.Parse("#DC3737"), 3),
                Fill = null,
                GeometrySize = 6
            }
        };

        NetProfitAxisX = new[] { new Axis { Labels = ordered.Select(x => x.ClosedDate?.ToString("MM-dd") ?? string.Empty).ToArray() } };
        NetProfitAxisY = new[] { new Axis { Labeler = value => value.ToString("N0") } };
    }

    private void BuildRevenueBreakdownChart(IReadOnlyCollection<BrandRevenue> brands)
    {
        RevenueBreakdownSeries = brands.Take(8)
            .Select(b => new PieSeries<decimal>
            {
                Name = b.Brand,
                Values = new[] { b.Revenue },
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.Coordinate.PrimaryValue:N0}"
            })
            .Cast<ISeries>()
            .ToArray();
    }

    private void BuildTopRepairsChart(IReadOnlyCollection<TicketProfit> profits)
    {
        var top = profits.OrderByDescending(x => x.Revenue - x.PartsCost).Take(5).ToList();
        TopRepairsSeries = new ISeries[]
        {
            new RowSeries<decimal>
            {
                Name = "Profit",
                Values = top.Select(x => x.Revenue - x.PartsCost).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("#2DC4B0"))
            }
        };

        TopRepairsAxisX = new[] { new Axis { Labeler = value => value.ToString("N0") } };
        TopRepairsAxisY = new[] { new Axis { Labels = top.Select(x => x.EscTicketId).ToArray() } };
    }
}
