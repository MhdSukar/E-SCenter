using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.IO;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly TicketsDataService _service;

        // ===== Summary metrics =====
        private int _totalTickets;
        public int TotalTickets { get => _totalTickets; set => SetProperty(ref _totalTickets, value); }

        private int _openTickets;
        public int OpenTickets { get => _openTickets; set => SetProperty(ref _openTickets, value); }

        private int _closedTickets;
        public int ClosedTickets { get => _closedTickets; set => SetProperty(ref _closedTickets, value); }

        private int _readyForPickupTickets;
        public int ReadyForPickupTickets { get => _readyForPickupTickets; set => SetProperty(ref _readyForPickupTickets, value); }

        // ===== Alerts =====
        private int _CriticalOpenTickets;
        public int CriticalOpenTickets { get => _CriticalOpenTickets; set => SetProperty(ref _CriticalOpenTickets, value); }

        private int _overdueTickets;
        public int OverdueTickets { get => _overdueTickets; set => SetProperty(ref _overdueTickets, value); }

        // ===== Recent tickets =====
        public ObservableCollection<RepairTicket> RecentTickets { get; } = new ObservableCollection<RepairTicket>();

        // ===== Parts risk counter =====
        public ObservableCollection<PartsRiskItem> LowPartSkus { get; } = new ObservableCollection<PartsRiskItem>();
        public ObservableCollection<PartsRiskItem> LowInventoryItems { get; } = new ObservableCollection<PartsRiskItem>();

        // ===== Commands =====
        public ICommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
            _service = new TicketsDataService(dbPath);

            RefreshCommand = new RelayCommand(_ => Refresh());

            Refresh();
        }

        private void OnTicketsChanged(object sender, EventArgs e)
        {
            // Refresh dashboard when tickets change
            Refresh();
        }

        private void OnDashboardRefreshRequested(object sender, EventArgs e)
        {
            // Refresh dashboard when requested
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                var all = _service.GetAll().ToList();

                TotalTickets = all.Count;
                ClosedTickets = all.Count(t => t.DeliveryDate.HasValue);
                OpenTickets = TotalTickets - ClosedTickets;
                ReadyForPickupTickets = all.Count(t => t.IsReadyForPickup && !t.DeliveryDate.HasValue);

                // Recent tickets: open or ready for pickup
                RecentTickets.Clear();
                foreach (var t in all.Where(t => !t.DeliveryDate.HasValue).OrderByDescending(t => t.ReceiveDate).Take(10))
                    RecentTickets.Add(t);

                PartsRiskCounter();

                // Alerts
                CriticalOpenTickets = all.Count(t => string.Equals(t.PriorityLevel, "Critical", StringComparison.OrdinalIgnoreCase) && !t.DeliveryDate.HasValue);
                OverdueTickets = all.Count(t => !t.DeliveryDate.HasValue && (DateTime.Now - t.ReceiveDate).TotalDays > 2);

                AppLogger.Info("Dashboard refreshed.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to refresh dashboard: {ex.Message}");
            }
        }

        private void PartsRiskCounter()
        {
            LowPartSkus.Clear();
            LowInventoryItems.Clear();

            var partsRepo = new PartsRepository();
            var inventoryRepo = new InventoryRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql"));

            var lowParts = partsRepo.GetAll()
                .Where(p => p.QuantityOnHand < 5 && !string.IsNullOrWhiteSpace(p.SKU))
                .OrderBy(p => p.QuantityOnHand);
            foreach (var part in lowParts)
            {
                LowPartSkus.Add(new PartsRiskItem
                {
                    Label = part.SKU.Trim(),
                    Quantity = part.QuantityOnHand
                });
            }

            var lowInventory = inventoryRepo.GetAll()
                .Where(i => i.QuantityOnHand < 2)
                .OrderBy(i => i.QuantityOnHand);
            foreach (var item in lowInventory)
            {
                LowInventoryItems.Add(new PartsRiskItem
                {
                    Label = BuildInventoryLabel(item),
                    Quantity = item.QuantityOnHand
                });
            }
        }

        private static string BuildInventoryLabel(InventoryItemModel item)
        {
            var label = $"{item.ItemType} {item.Brand} {item.Model}".Trim();
            return string.IsNullOrWhiteSpace(label) ? "Inventory item" : label;
        }

        public class PartsRiskItem
        {
            public string Label { get; set; }
            public int Quantity { get; set; }
        }
    }
}
