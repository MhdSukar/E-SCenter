using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using System.Windows;
using ESCenter.Core;
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

        // ===== Priority distribution =====
        public ObservableCollection<PriorityStat> PriorityStats { get; } = new ObservableCollection<PriorityStat>();

        // ===== Commands =====
        public ICommand RefreshCommand { get; }
        public ICommand OpenTicketsCommand { get; }
        public ICommand ClosedTicketsCommand { get; }

        public DashboardViewModel()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
            _service = new TicketsDataService(dbPath);

            RefreshCommand = new RelayCommand(_ => Refresh());
            OpenTicketsCommand = new RelayCommand(param => ExecuteShowRepairTickets(param));
            ClosedTicketsCommand = new RelayCommand(param => ExecuteShowRepairTickets(param));



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

                // Priority distribution (open tickets only)
                PriorityStats.Clear();
                var priorities = new[] { "Critical", "Major", "Normal", "Minor" };
                foreach (var p in priorities)
                {
                    var count = all.Count(t => string.Equals(t.PriorityLevel, p, StringComparison.OrdinalIgnoreCase) && !t.DeliveryDate.HasValue);
                    PriorityStats.Add(new PriorityStat { Priority = p, Count = count });
                }

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

        private void ExecuteShowRepairTickets(object parameter)
        {
            try
            {
                var mainVm = System.Windows.Application.Current?.MainWindow?.DataContext;
                if (mainVm == null) return;

                var prop = mainVm.GetType().GetProperty("ShowRepairTicketsCommand", BindingFlags.Public | BindingFlags.Instance);
                var cmd = prop?.GetValue(mainVm) as ICommand;
                if (cmd != null && cmd.CanExecute(parameter))
                    cmd.Execute(parameter);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Failed to forward navigation command: {ex.Message}");
            }
        }


        public class PriorityStat
        {
            public string Priority { get; set; }
            public int Count { get; set; }
        }
    }
}