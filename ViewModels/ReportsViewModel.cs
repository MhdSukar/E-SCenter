using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ESCenter.ViewModels
{
    public enum ReportPeriod
    {
        Today,
        ThisWeek,
        ThisMonth,
        LastMonth,
        Custom
    }

    public sealed class ReportsViewModel : ObservableObject, IDisposable
    {
        private EventHandler? _databasePathChangedHandler;
        private bool _isApplyingPeriod;

        private static readonly Dictionary<string, ReportPeriod> PeriodMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Today"] = ReportPeriod.Today,
            ["This Week"] = ReportPeriod.ThisWeek,
            ["This Month"] = ReportPeriod.ThisMonth,
            ["Last Month"] = ReportPeriod.LastMonth,
            ["Custom"] = ReportPeriod.Custom
        };

        public ObservableCollection<RevenueByDayItem> RevenueByDay { get; } = new();
        public ObservableCollection<TopRepairItem> TopRepairs { get; } = new();
        public ObservableCollection<TopBrandItem> TopBrands { get; } = new();
        public ObservableCollection<StatusBreakdownItem> StatusBreakdown { get; } = new();

        public IReadOnlyList<string> PeriodOptions { get; } = new[] { "Today", "This Week", "This Month", "Last Month", "Custom" };

        private ReportPeriod _selectedPeriod = ReportPeriod.ThisMonth;
        public ReportPeriod SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (SetProperty(ref _selectedPeriod, value) && !_isApplyingPeriod)
                {
                    LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync));
                }
            }
        }

        private string _selectedPeriodOption = "This Month";
        public string SelectedPeriodOption
        {
            get => _selectedPeriodOption;
            set
            {
                if (SetProperty(ref _selectedPeriodOption, value))
                {
                    PeriodChangedCommand.Execute(value);
                }
            }
        }

        private DateTime _periodStart = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime PeriodStart
        {
            get => _periodStart;
            set
            {
                if (SetProperty(ref _periodStart, value) && SelectedPeriod == ReportPeriod.Custom && !_isApplyingPeriod)
                {
                    LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync));
                }
            }
        }

        private DateTime _periodEnd = DateTime.Today;
        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set
            {
                if (SetProperty(ref _periodEnd, value) && SelectedPeriod == ReportPeriod.Custom && !_isApplyingPeriod)
                {
                    LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync));
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private decimal _totalRevenueSP;
        public decimal TotalRevenueSP
        {
            get => _totalRevenueSP;
            set => SetProperty(ref _totalRevenueSP, value);
        }

        private decimal _totalRevenueUSD;
        public decimal TotalRevenueUSD
        {
            get => _totalRevenueUSD;
            set => SetProperty(ref _totalRevenueUSD, value);
        }

        private int _totalTicketsInPeriod;
        public int TotalTicketsInPeriod
        {
            get => _totalTicketsInPeriod;
            set => SetProperty(ref _totalTicketsInPeriod, value);
        }

        private int _closedTicketsInPeriod;
        public int ClosedTicketsInPeriod
        {
            get => _closedTicketsInPeriod;
            set => SetProperty(ref _closedTicketsInPeriod, value);
        }

        private double _completionRate;
        public double CompletionRate
        {
            get => _completionRate;
            set => SetProperty(ref _completionRate, value);
        }

        private ISeries[] _revenueChartSeries = Array.Empty<ISeries>();
        public ISeries[] RevenueChartSeries
        {
            get => _revenueChartSeries;
            set => SetProperty(ref _revenueChartSeries, value);
        }

        private Axis[] _revenueChartXAxes = Array.Empty<Axis>();
        public Axis[] RevenueChartXAxes
        {
            get => _revenueChartXAxes;
            set => SetProperty(ref _revenueChartXAxes, value);
        }

        private ISeries[] _statusChartSeries = Array.Empty<ISeries>();
        public ISeries[] StatusChartSeries
        {
            get => _statusChartSeries;
            set => SetProperty(ref _statusChartSeries, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportReportCsvCommand { get; }
        public ICommand PeriodChangedCommand { get; }

        public ReportsViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync)));
            ExportReportCsvCommand = new RelayCommand(_ => ExportReportCsvAsync().FireAndForget(nameof(ExportReportCsvAsync)));
            PeriodChangedCommand = new RelayCommand(param => ApplyPeriod(param?.ToString()));

            if (!DesignTimeHelper.IsInDesignMode)
            {
                _databasePathChangedHandler = (_, _) => LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync));
                DatabasePathService.DatabasePathChanged += _databasePathChangedHandler;
                LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync));
            }
        }

        private void ApplyPeriod(string? periodValue)
        {
            if (string.IsNullOrWhiteSpace(periodValue) || !PeriodMap.TryGetValue(periodValue.Trim(), out var parsedPeriod))
            {
                return;
            }

            _isApplyingPeriod = true;
            try
            {
                var today = DateTime.Today;
                SelectedPeriod = parsedPeriod;

                switch (parsedPeriod)
                {
                    case ReportPeriod.Today:
                        PeriodStart = today;
                        PeriodEnd = today;
                        break;
                    case ReportPeriod.ThisWeek:
                        var diff = ((int)today.DayOfWeek + 6) % 7;
                        PeriodStart = today.AddDays(-diff);
                        PeriodEnd = today;
                        break;
                    case ReportPeriod.ThisMonth:
                        PeriodStart = new DateTime(today.Year, today.Month, 1);
                        PeriodEnd = today;
                        break;
                    case ReportPeriod.LastMonth:
                        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
                        PeriodStart = firstDayThisMonth.AddMonths(-1);
                        PeriodEnd = firstDayThisMonth.AddDays(-1);
                        break;
                    case ReportPeriod.Custom:
                        break;
                }

                SelectedPeriodOption = periodValue.Trim();
            }
            finally
            {
                _isApplyingPeriod = false;
            }

            LoadReportsAsync().FireAndForget(nameof(LoadReportsAsync));
        }

        public async Task LoadReportsAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading reports...";

            try
            {
                var startStr = PeriodStart.Date.ToString("o", CultureInfo.InvariantCulture);
                var endStr = PeriodEnd.Date.AddDays(1).ToString("o", CultureInfo.InvariantCulture);

                var revenueTask = QueryRevenueByDayAsync(startStr, endStr);
                var topRepairsTask = QueryTopRepairsAsync(startStr, endStr);
                var topBrandsTask = QueryTopBrandsAsync(startStr, endStr);
                var statusBreakdownTask = QueryStatusBreakdownAsync();
                var totalsTask = QueryTotalsAsync(startStr, endStr);

                await Task.WhenAll(revenueTask, topRepairsTask, topBrandsTask, statusBreakdownTask, totalsTask);

                var revenueRows = await revenueTask;
                var topRepairRows = await topRepairsTask;
                var topBrandRows = await topBrandsTask;
                var statusRows = await statusBreakdownTask;
                var totals = await totalsTask;

                RevenueByDay.Clear();
                foreach (var item in revenueRows)
                {
                    RevenueByDay.Add(item);
                }

                TopRepairs.Clear();
                var topRepairTotal = topRepairRows.Sum(r => r.Count);
                foreach (var item in topRepairRows)
                {
                    item.Percentage = topRepairTotal == 0 ? 0 : item.Count / (double)topRepairTotal * 100.0;
                    TopRepairs.Add(item);
                }

                TopBrands.Clear();
                foreach (var item in topBrandRows)
                {
                    TopBrands.Add(item);
                }

                StatusBreakdown.Clear();
                foreach (var item in statusRows)
                {
                    StatusBreakdown.Add(item);
                }

                TotalTicketsInPeriod = totals.Total;
                ClosedTicketsInPeriod = totals.Closed;
                TotalRevenueSP = totals.RevSP;
                TotalRevenueUSD = totals.RevUSD;
                CompletionRate = TotalTicketsInPeriod == 0
                    ? 0
                    : ClosedTicketsInPeriod / (double)TotalTicketsInPeriod * 100.0;

                BuildRevenueChart();
                BuildStatusChart();

                StatusMessage = $"Reports refreshed ({DateTime.Now:HH:mm:ss}).";
                AppLogger.Success("Reports loaded successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load reports: {ex.Message}");
                StatusMessage = "Failed to load reports.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task<List<RevenueByDayItem>> QueryRevenueByDayAsync(string startStr, string endStr)
            => Task.Run(() =>
            {
                const string sql = @"SELECT DATE(DeliveryDate) as Day,
       SUM(CASE WHEN UPPER(IFNULL(FinalCostCurrency,'S.P'))='S.P'
                THEN IFNULL(FinalCost,0) ELSE 0 END) as SP,
       SUM(CASE WHEN UPPER(IFNULL(FinalCostCurrency,'S.P'))='USD'
                THEN IFNULL(FinalCost,0) ELSE 0 END) as USD
FROM TicketsDB
WHERE DeliveryDate >= @start AND DeliveryDate < @end
GROUP BY DATE(DeliveryDate)
ORDER BY Day ASC;";

                var rows = new List<RevenueByDayItem>();
                using var conn = CreateConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@start", startStr);
                cmd.Parameters.AddWithValue("@end", endStr);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new RevenueByDayItem
                    {
                        Day = reader["Day"]?.ToString() ?? string.Empty,
                        AmountSP = ConvertToDecimal(reader["SP"]),
                        AmountUSD = ConvertToDecimal(reader["USD"])
                    });
                }

                return rows;
            });

        private Task<List<TopRepairItem>> QueryTopRepairsAsync(string startStr, string endStr)
            => Task.Run(() =>
            {
                const string sql = @"SELECT IFNULL(DeviceCategory,'Unknown') as Cat, COUNT(*) as Cnt
FROM TicketsDB
WHERE ReceiveDate >= @start AND ReceiveDate < @end
GROUP BY Cat ORDER BY Cnt DESC LIMIT 8;";

                var rows = new List<TopRepairItem>();
                using var conn = CreateConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@start", startStr);
                cmd.Parameters.AddWithValue("@end", endStr);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new TopRepairItem
                    {
                        Category = reader["Cat"]?.ToString() ?? "Unknown",
                        Count = ConvertToInt(reader["Cnt"])
                    });
                }

                return rows;
            });

        private Task<List<TopBrandItem>> QueryTopBrandsAsync(string startStr, string endStr)
            => Task.Run(() =>
            {
                const string sql = @"SELECT IFNULL(DeviceBrand,'Unknown') as Brand, COUNT(*) as Cnt
FROM TicketsDB
WHERE ReceiveDate >= @start AND ReceiveDate < @end
GROUP BY Brand ORDER BY Cnt DESC LIMIT 8;";

                var rows = new List<TopBrandItem>();
                using var conn = CreateConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@start", startStr);
                cmd.Parameters.AddWithValue("@end", endStr);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new TopBrandItem
                    {
                        Brand = reader["Brand"]?.ToString() ?? "Unknown",
                        Count = ConvertToInt(reader["Cnt"])
                    });
                }

                return rows;
            });

        private Task<List<StatusBreakdownItem>> QueryStatusBreakdownAsync()
            => Task.Run(() =>
            {
                const string sql = @"SELECT IFNULL(RepairStatus,'Unknown') as Status, COUNT(*) as Cnt
FROM TicketsDB GROUP BY Status ORDER BY Cnt DESC;";

                var rows = new List<StatusBreakdownItem>();
                using var conn = CreateConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var status = reader["Status"]?.ToString() ?? "Unknown";
                    rows.Add(new StatusBreakdownItem
                    {
                        Status = status,
                        Count = ConvertToInt(reader["Cnt"]),
                        HexColor = GetStatusColor(status)
                    });
                }

                return rows;
            });

        private Task<TotalsRow> QueryTotalsAsync(string startStr, string endStr)
            => Task.Run(() =>
            {
                const string sql = @"SELECT COUNT(*) as Total,
       SUM(CASE WHEN DeliveryDate IS NOT NULL THEN 1 ELSE 0 END) as Closed,
       SUM(CASE WHEN UPPER(IFNULL(FinalCostCurrency,'S.P'))='S.P'
                AND DeliveryDate >= @start AND DeliveryDate < @end
                THEN IFNULL(FinalCost,0) ELSE 0 END) as RevSP,
       SUM(CASE WHEN UPPER(IFNULL(FinalCostCurrency,'S.P'))='USD'
                AND DeliveryDate >= @start AND DeliveryDate < @end
                THEN IFNULL(FinalCost,0) ELSE 0 END) as RevUSD
FROM TicketsDB
WHERE ReceiveDate >= @start AND ReceiveDate < @end;";

                using var conn = CreateConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@start", startStr);
                cmd.Parameters.AddWithValue("@end", endStr);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return new TotalsRow();
                }

                return new TotalsRow
                {
                    Total = ConvertToInt(reader["Total"]),
                    Closed = ConvertToInt(reader["Closed"]),
                    RevSP = ConvertToDecimal(reader["RevSP"]),
                    RevUSD = ConvertToDecimal(reader["RevUSD"])
                };
            });

        private static SQLiteConnection CreateConnection()
        {
            var conn = new SQLiteConnection($"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;");
            conn.Open();
            return conn;
        }

        private void BuildRevenueChart()
        {
            var spValues = RevenueByDay.Select(x => x.AmountSP).ToArray();
            var usdValues = RevenueByDay.Select(x => x.AmountUSD).ToArray();

            RevenueChartSeries = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Name = "S.P",
                    Values = spValues,
                    Stroke = new SolidColorPaint(new SKColor(0, 229, 212), 2.5f),
                    Fill = null,
                    GeometrySize = 5
                },
                new LineSeries<decimal>
                {
                    Name = "USD",
                    Values = usdValues,
                    Stroke = new SolidColorPaint(new SKColor(46, 255, 46), 2.5f),
                    Fill = null,
                    GeometrySize = 5
                }
            };

            RevenueChartXAxes = new[]
            {
                new Axis
                {
                    Labels = RevenueByDay.Select(r => r.Day).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 10
                }
            };
        }

        private void BuildStatusChart()
        {
            StatusChartSeries = StatusBreakdown
                .Select(s =>
                {
                    var parsed = SKColor.TryParse(s.HexColor, out var color) ? color : SKColors.Gray;
                    return (ISeries)new PieSeries<int>
                    {
                        Name = s.Status,
                        Values = new[] { s.Count },
                        Fill = new SolidColorPaint(parsed)
                    };
                })
                .ToArray();
        }

        private async Task ExportReportCsvAsync()
        {
            await CsvExportService.ExportAsync(RevenueByDay.Cast<object>(),
                new[] { "Date", "Revenue (S.P)", "Revenue (USD)" },
                row =>
                {
                    var revenue = (RevenueByDayItem)row;
                    return new[] { revenue.Day, revenue.AmountSP.ToString("0.##"), revenue.AmountUSD.ToString("0.##") };
                },
                "revenue-report.csv");
        }

        private static int ConvertToInt(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static decimal ConvertToDecimal(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0m;
            }

            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static string GetStatusColor(string status)
            => status switch
            {
                "Received" => "#5AA7FF",
                "In Repair" => "#00E5D4",
                "Waiting for Parts" => "#FBBF24",
                "Completed" => "#22C55E",
                "Cancelled" => "#EF4444",
                _ => "#808080"
            };

        public void Dispose()
        {
            if (_databasePathChangedHandler != null)
            {
                DatabasePathService.DatabasePathChanged -= _databasePathChangedHandler;
                _databasePathChangedHandler = null;
            }
        }

        private sealed class TotalsRow
        {
            public int Total { get; init; }
            public int Closed { get; init; }
            public decimal RevSP { get; init; }
            public decimal RevUSD { get; init; }
        }
    }

    public sealed class RevenueByDayItem
    {
        public string Day { get; set; } = string.Empty;
        public decimal AmountSP { get; set; }
        public decimal AmountUSD { get; set; }
    }

    public sealed class TopRepairItem
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public sealed class TopBrandItem
    {
        public string Brand { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class StatusBreakdownItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public string HexColor { get; set; } = "#808080";
    }
}
