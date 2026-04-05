using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly TicketsDataService _service;
        private readonly PartsRepository _partsRepository;
        private readonly InventoryRepository _inventoryRepository;

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

        private decimal? _weeklyIncome;
        public decimal? WeeklyIncome { get => _weeklyIncome; set => SetProperty(ref _weeklyIncome, value); }

        private double _weeklyCompletionPercent;
        public double WeeklyCompletionPercent { get => _weeklyCompletionPercent; set => SetProperty(ref _weeklyCompletionPercent, value); }

        private string _partsLowStockThreshold = "3";
        public string PartsLowStockThreshold
        {
            get => _partsLowStockThreshold;
            set
            {
                if (SetProperty(ref _partsLowStockThreshold, value))
                {
                    PersistLowStockThresholds();
                    _ = UpdateLowStockCounterAsync();
                }
            }
        }

        private string _inventoryLowStockThreshold = "3";
        public string InventoryLowStockThreshold
        {
            get => _inventoryLowStockThreshold;
            set
            {
                if (SetProperty(ref _inventoryLowStockThreshold, value))
                {
                    PersistLowStockThresholds();
                    _ = UpdateLowStockCounterAsync();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<RepairTicket> RecentTickets { get; } = new();
        public ObservableCollection<PriorityStat> PriorityStats { get; } = new();
        public ObservableCollection<LowStockCounterItem> LowStockItems { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenTicketsCommand { get; }
        public ICommand ClosedTicketsCommand { get; }

        public DashboardViewModel()
        {
            _service = new TicketsDataService();
            _partsRepository = new PartsRepository();
            _inventoryRepository = new InventoryRepository();

            var thresholds = UserPreferencesService.GetLowStockThresholds();
            _partsLowStockThreshold = thresholds.PartsThreshold.ToString();
            _inventoryLowStockThreshold = thresholds.InventoryThreshold.ToString();

            RefreshCommand = new RelayCommand(_ => Refresh());
            OpenTicketsCommand = new RelayCommand(param => ExecuteShowRepairTickets(param));
            ClosedTicketsCommand = new RelayCommand(param => ExecuteShowRepairTickets(param));

            TicketEvents.TicketsChanged += (_, _) => Refresh();
            DatabasePathService.DatabasePathChanged += (_, _) => Refresh();

            Refresh();
        }

        // Synchronous wrapper kept for compatibility (fires-and-forgets the async path)
        public void Refresh() => _ = RefreshAsync();

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
                WeeklyIncome = summary.WeeklyIncome;

                RecentTickets.Clear();
                foreach (var ticket in recentTicketsTask.Result)
                {
                    RecentTickets.Add(ticket);
                }

                PriorityStats.Clear();
                var priorityCounts = priorityCountsTask.Result;
                foreach (var priority in new[] { "Critical", "Major", "Normal", "Minor" })
                {
                    priorityCounts.TryGetValue(priority, out var count);
                    PriorityStats.Add(new PriorityStat { Priority = priority, Count = count });
                }

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

        private void PersistLowStockThresholds()
        {
            try
            {
                var partsThreshold = ParseThreshold(PartsLowStockThreshold);
                var inventoryThreshold = ParseThreshold(InventoryLowStockThreshold);
                UserPreferencesService.SetLowStockThresholds(partsThreshold, inventoryThreshold);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Failed to persist low stock thresholds: {ex.Message}");
            }
        }

        private async Task UpdateLowStockCounterAsync()
        {
            try
            {
                var partsThreshold = ParseThreshold(PartsLowStockThreshold);
                var inventoryThreshold = ParseThreshold(InventoryLowStockThreshold);

                var allParts = await _partsRepository.GetAllAsync();
                var allInventory = await _inventoryRepository.GetAllAsync();

                var partItems = allParts
                    .Where(p => p.QuantityOnHand <= partsThreshold)
                    .Select(p => new LowStockCounterItem
                    {
                        Source = "Parts",
                        Sku = p.SKU ?? string.Empty,
                        Name = string.IsNullOrWhiteSpace(p.PartCode)
                            ? string.IsNullOrWhiteSpace(p.SKU) ? "Unnamed Part" : p.SKU
                            : p.PartCode,
                        Quantity = p.QuantityOnHand
                    });

                var inventoryItems = allInventory
                    .Where(i => i.QuantityOnHand <= inventoryThreshold)
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

        private static int ParseThreshold(string input)
        {
            if (!int.TryParse(input, out var parsedValue))
            {
                return 0;
            }

            return Math.Max(0, parsedValue);
        }

        private void ExecuteShowRepairTickets(object parameter)
        {
            try
            {
                AppEvents.RequestNavigateToTickets();
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Failed to navigate to repair tickets: {ex.Message}");
            }
        }

        public class PriorityStat
        {
            public string Priority { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class LowStockCounterItem
        {
            public string Source { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}
