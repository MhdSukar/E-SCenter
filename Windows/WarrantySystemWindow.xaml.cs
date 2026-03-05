using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class WarrantySystemWindow : Window
    {
        private readonly TicketsDataService _ticketsDataService = new();

        public ObservableCollection<WarrantyDeviceRow> WarrantyItems { get; } = new();

        public WarrantySystemWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadWarrantyItems();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWarrantyItems();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            AdjustWindowSize();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AdjustWindowSize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                btnMaximize.Content = "\\u25A1";
            }
            else
            {
                WindowState = WindowState.Maximized;
                btnMaximize.Content = "\\u25A1";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                AdjustWindowSize();
            }
            else
            {
                DragMove();
            }
        }

        private void LoadWarrantyItems()
        {
            WarrantyItems.Clear();

            var tickets = _ticketsDataService.GetAll()
                .Where(t => t.HasWarranty)
                .OrderByDescending(t => t.ReceiveDate)
                .ToList();

            foreach (var ticket in tickets)
            {
                if (!WarrantyEvaluator.TryGetExpiration(ticket, out var expiresAt))
                {
                    continue;
                }

                var daysLeft = (expiresAt.Date - DateTime.Today).Days;

                WarrantyItems.Add(new WarrantyDeviceRow
                {
                    TicketCode = ticket.EscTicketId ?? $"ESC-{ticket.TicketId:D6}",
                    CustomerName = ticket.CustomerName ?? "-",
                    DeviceName = BuildDeviceName(ticket),
                    SerialNumber = string.IsNullOrWhiteSpace(ticket.SerialIMEI) ? "-" : ticket.SerialIMEI,
                    WarrantyPeriod = string.IsNullOrWhiteSpace(ticket.WarrantyPeriod) ? "-" : ticket.WarrantyPeriod,
                    WarrantyStartDateText = (ticket.DeliveryDate?.Date ?? ticket.ReceiveDate.Date).ToString("yyyy-MM-dd"),
                    WarrantyEndDateText = expiresAt.ToString("yyyy-MM-dd"),
                    RemainingText = daysLeft >= 0 ? $"{daysLeft} day(s)" : $"Expired {-daysLeft} day(s) ago",
                    WarrantyState = daysLeft >= 0 ? "Active" : "Expired"
                });
            }
        }

        private static string BuildDeviceName(RepairTicket ticket)
        {
            var parts = new[] { ticket.DeviceCategory, ticket.DeviceBrand, ticket.DeviceModel }
                .Where(value => !string.IsNullOrWhiteSpace(value));

            var combined = string.Join(" / ", parts);
            return string.IsNullOrWhiteSpace(combined) ? "Unknown Device" : combined;
        }
    }

    public class WarrantyDeviceRow
    {
        public string TicketCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string WarrantyPeriod { get; set; } = string.Empty;
        public string WarrantyStartDateText { get; set; } = string.Empty;
        public string WarrantyEndDateText { get; set; } = string.Empty;
        public string RemainingText { get; set; } = string.Empty;
        public string WarrantyState { get; set; } = string.Empty;
    }
}
