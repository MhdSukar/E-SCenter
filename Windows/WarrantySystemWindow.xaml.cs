using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using ESCenter.Core;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class WarrantySystemWindow : Window, INotifyPropertyChanged
    {
        private const int ExpiringSoonThresholdDays = 30;
        private readonly TicketsDataService _ticketsDataService = new();
        private bool _animatingClose;
        private bool _wasMinimized;
        private string _searchText = string.Empty;

        public ObservableCollection<WarrantyDeviceRow> WarrantyItems { get; } = new();
        public ObservableCollection<ExpiringWarrantyRow> ExpiringSoonItems { get; } = new();
        public int ExpiringWarrantyCount => ExpiringSoonItems.Count;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                CollectionViewSource.GetDefaultView(WarrantyItems)?.Refresh();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public WarrantySystemWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
            DataContext = this;
            CollectionViewSource.GetDefaultView(WarrantyItems).Filter = MatchesSearch;
            Loaded += (_, __) =>
            {
                WindowFader.SlideIn(this);
                RefreshWarrantyData();
            };
            Closing += Window_Closing;
            StateChanged += Window_StateChanged;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            WindowFader.SlideOut(this, () =>
            {
                Hide();
                if (Content is UIElement c)
                {
                    c.RenderTransform = Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshWarrantyData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowFader.FadeMinimize(this);
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _wasMinimized = true;
                return;
            }

            if (_wasMinimized && WindowState == WindowState.Normal)
            {
                _wasMinimized = false;
                WindowFader.FadeRestore(this);
            }
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
                SetMaximizeButtonIcon("Maximize");
            }
            else
            {
                WindowState = WindowState.Maximized;
                SetMaximizeButtonIcon("Restore");
            }
        }


        private void SetMaximizeButtonIcon(string resourceKey)
        {
            btnMaximize.Content = new System.Windows.Controls.Image
            {
                Source = (System.Windows.Media.ImageSource)FindResource(resourceKey),
                Width = 14,
                Height = 14,
                Stretch = System.Windows.Media.Stretch.Uniform
            };
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

        private void RefreshWarrantyData()
        {
            LoadWarrantyItems();
            LoadExpiringSoonItems();
            CollectionViewSource.GetDefaultView(WarrantyItems)?.Refresh();
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
                var warrantyState = daysLeft < 0
                    ? "Expired"
                    : daysLeft <= ExpiringSoonThresholdDays
                        ? "Expiring Soon"
                        : "Active";

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
                    WarrantyState = warrantyState
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

        private bool MatchesSearch(object candidate)
        {
            if (candidate is not WarrantyDeviceRow row)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var search = SearchText.Trim();
            return Contains(row.TicketCode, search)
                || Contains(row.CustomerName, search)
                || Contains(row.DeviceName, search)
                || Contains(row.SerialNumber, search)
                || Contains(row.WarrantyState, search);
        }

        private static bool Contains(string value, string search) =>
            !string.IsNullOrWhiteSpace(value)
            && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        private void LoadExpiringSoonItems()
        {
            ExpiringSoonItems.Clear();

            var tickets = _ticketsDataService.GetAll();
            var expiringSoon = WarrantyEvaluator.GetExpiringWarranties(tickets, ExpiringSoonThresholdDays)
                .OrderBy(t =>
                {
                    WarrantyEvaluator.TryGetExpiration(t, out var expiration);
                    return expiration;
                })
                .ToList();

            foreach (var ticket in expiringSoon)
            {
                if (!WarrantyEvaluator.TryGetExpiration(ticket, out var expiration))
                {
                    continue;
                }

                var daysRemaining = (expiration.Date - DateTime.Today).Days;
                ExpiringSoonItems.Add(new ExpiringWarrantyRow
                {
                    ClientName = string.IsNullOrWhiteSpace(ticket.CustomerName) ? "-" : ticket.CustomerName,
                    DeviceName = BuildDeviceName(ticket),
                    DaysRemainingText = $"{daysRemaining} day(s)",
                    IsUrgent = daysRemaining <= 7
                });
            }

            OnPropertyChanged(nameof(ExpiringWarrantyCount));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    public class ExpiringWarrantyRow
    {
        public string ClientName { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DaysRemainingText { get; set; } = string.Empty;
        public bool IsUrgent { get; set; }
    }
}
