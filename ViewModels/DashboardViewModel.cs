using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly TicketsDataService _service;

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

        private double _weeklyIncome;
        public double WeeklyIncome { get => _weeklyIncome; set => SetProperty(ref _weeklyIncome, value); }

        private double _weeklyCompletionPercent;
        public double WeeklyCompletionPercent { get => _weeklyCompletionPercent; set => SetProperty(ref _weeklyCompletionPercent, value); }

        public ObservableCollection<RepairTicket> RecentTickets { get; } = new();
        public ObservableCollection<PriorityStat> PriorityStats { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenTicketsCommand { get; }
        public ICommand ClosedTicketsCommand { get; }

        public DashboardViewModel()
        {
            _service = new TicketsDataService();

            RefreshCommand = new RelayCommand(_ => Refresh());
            OpenTicketsCommand = new RelayCommand(param => ExecuteShowRepairTickets(param));
            ClosedTicketsCommand = new RelayCommand(param => ExecuteShowRepairTickets(param));

            TicketEvents.TicketsChanged += (_, _) => Refresh();
            DatabasePathService.DatabasePathChanged += (_, _) => Refresh();

            Refresh();
        }

        public void Refresh()
        {
            try
            {
                var all = _service.GetAll().ToList();
                var now = DateTime.Now;
                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);

                TotalTickets = all.Count;
                ClosedTickets = all.Count(t => t.DeliveryDate.HasValue);
                OpenTickets = TotalTickets - ClosedTickets;
                ReadyForPickupTickets = all.Count(t => t.IsReadyForPickup && !t.DeliveryDate.HasValue);

                RecentTickets.Clear();
                foreach (var ticket in all.Where(t => !t.DeliveryDate.HasValue).OrderByDescending(t => t.ReceiveDate).Take(10))
                {
                    RecentTickets.Add(ticket);
                }

                PriorityStats.Clear();
                foreach (var priority in new[] { "Critical", "Major", "Normal", "Minor" })
                {
                    var count = all.Count(t => string.Equals(t.PriorityLevel, priority, StringComparison.OrdinalIgnoreCase) && !t.DeliveryDate.HasValue);
                    PriorityStats.Add(new PriorityStat { Priority = priority, Count = count });
                }

                CriticalOpenTickets = all.Count(t => string.Equals(t.PriorityLevel, "Critical", StringComparison.OrdinalIgnoreCase) && !t.DeliveryDate.HasValue);
                OverdueTickets = all.Count(t => !t.DeliveryDate.HasValue && (now - t.ReceiveDate).TotalDays > 2);

                WeeklyTotalTickets = all.Count(t => t.ReceiveDate >= weekStart && t.ReceiveDate < weekEnd);
                WeeklyFinishedTickets = all.Count(t => t.DeliveryDate.HasValue && t.DeliveryDate.Value >= weekStart && t.DeliveryDate.Value < weekEnd);
                WeeklyIncome = all
                    .Where(t => t.DeliveryDate.HasValue && t.DeliveryDate.Value >= weekStart && t.DeliveryDate.Value < weekEnd)
                    .Sum(t => t.FinalCost);

                WeeklyCompletionPercent = WeeklyTotalTickets == 0
                    ? 0
                    : (double)WeeklyFinishedTickets / WeeklyTotalTickets * 100.0;

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
                if (mainVm == null)
                {
                    return;
                }

                var property = mainVm.GetType().GetProperty("ShowRepairTicketsCommand", BindingFlags.Public | BindingFlags.Instance);
                var command = property?.GetValue(mainVm) as ICommand;
                if (command != null && command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Failed to forward navigation command: {ex.Message}");
            }
        }

        public class PriorityStat
        {
            public string Priority { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}
