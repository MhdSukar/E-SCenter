using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class DashboardViewModel : ObservableObject, IDisposable
    {
        private readonly TicketsDataService _service;
        private readonly PartsRepository _partsRepository;
        private readonly InventoryRepository _inventoryRepository;
        private EventHandler? _ticketsChangedHandler;
        private EventHandler? _databasePathChangedHandler;

        private int _totalTickets;
        public int TotalTickets { get => _totalTickets; set => SetProperty(ref _totalTickets, value); }

        private int _openTickets;
        public int OpenTickets { get => _openTickets; set => SetProperty(ref _openTickets, value); }

        private int _closedTickets;
        public int ClosedTickets { get => _closedTickets; set => SetProperty(ref _closedTickets, value); }

        private int _readyForPickupTickets;
        public int ReadyForPickupTickets { get => _readyForPickupTickets; set => SetProperty(ref _readyForPickupTickets, value); }

        private int _criticalOpenTickets;
        public int CriticalOpenTickets { get => _criticalOpenTickets; set => SetProperty(ref _criticalOpenTickets, value); }

        private int _overdueTickets;
        public int OverdueTickets { get => _overdueTickets; set => SetProperty(ref _overdueTickets, value); }

        private int _weeklyTotalTickets;
        public int WeeklyTotalTickets { get => _weeklyTotalTickets; set => SetProperty(ref _weeklyTotalTickets, value); }

        private int _weeklyFinishedTickets;
        public int WeeklyFinishedTickets { get => _weeklyFinishedTickets; set => SetProperty(ref _weeklyFinishedTickets, value); }

        private decimal _weeklyIncomeSP;
        public decimal WeeklyIncomeSP { get => _weeklyIncomeSP; set => SetProperty(ref _weeklyIncomeSP, value); }

        private decimal _weeklyIncomeUSD;
        public decimal WeeklyIncomeUSD { get => _weeklyIncomeUSD; set => SetProperty(ref _weeklyIncomeUSD, value); }

        private double _weeklyCompletionPercent;
        public double WeeklyCompletionPercent { get => _weeklyCompletionPercent; set => SetProperty(ref _weeklyCompletionPercent, value); }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<RepairTicket> RecentTickets { get; } = new();
        public ObservableCollection<LowStockCounterItem> LowStockItems { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenTicketCommand { get; }
        public ICommand ExportReorderListCommand { get; }
        public ICommand RestockItemCommand { get; }
        public Action<RepairTicket>? TicketSelected;

        public DashboardViewModel()
        {
            var isDesignMode = DesignTimeHelper.IsInDesignMode;
            _service = AppServices.IsInitialized ? AppServices.Get<TicketsDataService>() : new TicketsDataService();
            _partsRepository = AppServices.IsInitialized ? AppServices.Get<PartsRepository>() : new PartsRepository();
            _inventoryRepository = AppServices.IsInitialized ? AppServices.Get<InventoryRepository>() : new InventoryRepository();

            RefreshCommand = new RelayCommand(_ => Refresh());
            OpenTicketCommand = new RelayCommand(param =>
            {
                if (param is RepairTicket ticket)
                {
                    TicketSelected?.Invoke(ticket);
                }
            });
            ExportReorderListCommand = new RelayCommand(_ => ExportReorderList().FireAndForget(nameof(ExportReorderList)));
            RestockItemCommand = new RelayCommand(param =>
            {
                if (param is not LowStockCounterItem item) return;

                var sku = item.Source == "Parts" ? item.Sku : string.Empty;
                RestockWizardHelper.Show(item.Name, item.Source, item.Quantity, sku);
                TicketEvents.RaiseStockChanged();
                Refresh();
            });

            if (isDesignMode)
            {
                TotalTickets = 124;
                OpenTickets = 38;
                ClosedTickets = 86;
                ReadyForPickupTickets = 11;
                CriticalOpenTickets = 4;
                OverdueTickets = 6;
                WeeklyTotalTickets = 19;
                WeeklyFinishedTickets = 14;
                WeeklyIncomeSP = 780m;
                WeeklyIncomeUSD = 0m;
                WeeklyCompletionPercent = 73.68;
                RecentTickets.Add(new RepairTicket { EscTicketId = "ESC-2401", CustomerName = "John Carter", DeviceModel = "Galaxy S22", PriorityLevel = "Major" });
                RecentTickets.Add(new RepairTicket { EscTicketId = "ESC-2402", CustomerName = "Mia Khan", DeviceModel = "iPhone 13", PriorityLevel = "Critical" });
                LowStockItems.Add(new LowStockCounterItem { Source = "Parts", Name = "Charging IC", Quantity = 2, Sku = "IC-CHG-08" });
                LowStockItems.Add(new LowStockCounterItem { Source = "Inventory", Name = "USB-C Cable", Quantity = 3, Sku = string.Empty });
                return;
            }

            _ticketsChangedHandler = (_, _) => Refresh();
            _databasePathChangedHandler = (_, _) => Refresh();
            TicketEvents.TicketsChanged += _ticketsChangedHandler;
            DatabasePathService.DatabasePathChanged += _databasePathChangedHandler;
            Refresh();
        }

        // Synchronous wrapper kept for compatibility (fires-and-forgets the async path)
        public void Refresh() => RefreshAsync().FireAndForget(nameof(RefreshAsync));

        public async Task RefreshAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var now = DateTime.Now;
                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);

                var summaryTask = _service.GetDashboardSummaryAsync(now, weekStart, weekEnd);
                var recentTicketsTask = _service.GetRecentOpenTicketsAsync(10);
                var priorityCountsTask = _service.GetOpenPriorityCountsAsync(new[] { "Critical", "Major", "Normal", "Minor" });

                await Task.WhenAll(summaryTask, recentTicketsTask, priorityCountsTask);

                var summary = summaryTask.Result;
                TotalTickets = summary.TotalTickets;
                OpenTickets = summary.OpenTickets;
                ClosedTickets = summary.ClosedTickets;
                ReadyForPickupTickets = summary.ReadyForPickupTickets;
                CriticalOpenTickets = summary.CriticalOpenTickets;
                OverdueTickets = summary.OverdueTickets;
                WeeklyTotalTickets = summary.WeeklyTotalTickets;
                WeeklyFinishedTickets = summary.WeeklyFinishedTickets;
                WeeklyIncomeSP = summary.WeeklyIncomeSP;
                WeeklyIncomeUSD = summary.WeeklyIncomeUSD;

                RecentTickets.Clear();
                foreach (var ticket in recentTicketsTask.Result)
                {
                    ticket.FinalCostCurrency = NormalizeCurrency(ticket.FinalCostCurrency);
                    RecentTickets.Add(ticket);
                }
                ApplyDashboardCostDisplayPreference();
                WeeklyCompletionPercent = WeeklyTotalTickets == 0
                    ? 0
                    : (double)WeeklyFinishedTickets / WeeklyTotalTickets * 100.0;

                await UpdateLowStockCounterAsync();
                AppLogger.Success("Dashboard Loaded.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to refresh dashboard: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }



        private async Task ExportReorderList()
        {
            try
            {
                var allParts = await _partsRepository.GetAllAsync();
                var allInventory = await _inventoryRepository.GetAllAsync();
                var rows = allParts.Where(x => x.QuantityOnHand == 0).Select(x => new[] { "Parts", x.SKU ?? x.PartCode ?? string.Empty, x.PartType ?? string.Empty, x.QuantityOnHand.ToString(), $"{x.LocationShelf}/{x.LocationBin}" })
                    .Concat(allInventory.Where(i => i.QuantityOnHand == 0).Select(i => new[] { "Inventory", i.ItemType ?? i.Model ?? string.Empty, i.ItemType ?? string.Empty, i.QuantityOnHand.ToString(), i.LocationBox ?? string.Empty }))
                    .ToList();

                await CsvExportService.ExportAsync(rows.Cast<object>(), new[] { "Source", "SKU/Name", "Type", "CurrentQty", "Shelf/Box" },
                    row => (string[])row, "reorder-list.csv");
                AppLogger.Success("Reorder list exported.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to export reorder list: {ex.Message}");
            }
        }

        private void ApplyDashboardCostDisplayPreference()
        {
            var preferredCurrency = UserPreferencesService.GetDashboardFinalCostCurrency();
            var rates = UserPreferencesService.GetExchangeRates();
            rates.TryGetValue("USD", out var usdRate);
            if (usdRate <= 0)
            {
                usdRate = 1m;
            }

            foreach (var ticket in RecentTickets)
            {
                ticket.DashboardCostDisplay = FormatDashboardCost(ticket.FinalCost, ticket.FinalCostCurrency, preferredCurrency, usdRate);
            }
        }

        private static string FormatDashboardCost(decimal? amount, string sourceCurrency, string targetCurrency, decimal usdRate)
        {
            if (!amount.HasValue)
            {
                return "-";
            }

            var normalizedSource = NormalizeCurrency(sourceCurrency);
            var normalizedTarget = NormalizeCurrency(targetCurrency);

            if (normalizedSource == normalizedTarget)
            {
                return $"{amount.Value:N0} {normalizedTarget}";
            }

            if (normalizedSource == "USD" && normalizedTarget == "S.P")
            {
                return $"{Math.Round(amount.Value * usdRate, 0):N0} S.P";
            }

            if (normalizedSource == "S.P" && normalizedTarget == "USD")
            {
                return $"{(amount.Value / usdRate).ToString("N2", CultureInfo.CurrentCulture)} USD";
            }

            return $"{amount.Value:N0} {normalizedSource}";
        }

        private static string NormalizeCurrency(string? currency)
            => string.Equals(currency?.Trim(), "USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "S.P";

        private async Task UpdateLowStockCounterAsync()
        {
            try
            {
                var thresholds = UserPreferencesService.GetLowStockThresholds();
                var allParts = await _partsRepository.GetAllAsync();
                var allInventory = await _inventoryRepository.GetAllAsync();

                var partItems = allParts
                    .Where(p => p.QuantityOnHand <= thresholds.PartsThreshold)
                    .Select(p => new LowStockCounterItem
                    {
                        Source = "Parts",
                        Sku = p.SKU ?? string.Empty,
                        Name = string.IsNullOrWhiteSpace(p.PartCode)
                            ? string.IsNullOrWhiteSpace(p.Description) ? "Unnamed Part" : p.Description
                            : p.PartCode,
                        Quantity = p.QuantityOnHand
                    });

                var inventoryItems = allInventory
                    .Where(i => i.QuantityOnHand <= thresholds.InventoryThreshold)
                    .Select(i => new LowStockCounterItem
                    {
                        Source = "Inventory",
                        Sku = string.Empty,
                        Name = string.IsNullOrWhiteSpace(i.ItemType)
                            ? string.IsNullOrWhiteSpace(i.Model) ? "Unnamed Inventory Item" : i.Model
                            : i.ItemType,
                        Quantity = i.QuantityOnHand
                    });

                var allLowStockItems = partItems
                    .Concat(inventoryItems)
                    .OrderBy(item => item.Quantity)
                    .ThenBy(item => item.Source)
                    .ThenBy(item => item.Name)
                    .ToList();

                LowStockItems.Clear();
                foreach (var lowStockItem in allLowStockItems)
                {
                    LowStockItems.Add(lowStockItem);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to update low stock counter: {ex.Message}");
            }
        }


        public void Dispose()
        {
            if (_ticketsChangedHandler != null)
            {
                TicketEvents.TicketsChanged -= _ticketsChangedHandler;
                _ticketsChangedHandler = null;
            }

            if (_databasePathChangedHandler != null)
            {
                DatabasePathService.DatabasePathChanged -= _databasePathChangedHandler;
                _databasePathChangedHandler = null;
            }
        }
    }
}
