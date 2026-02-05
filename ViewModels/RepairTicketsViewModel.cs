using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class RepairTicketsViewModel : ObservableObject
    {
        private readonly TicketsDataService _service;

        // =========================================================
        // COLLECTIONS
        // =========================================================

        public ObservableCollection<RepairTicket> Tickets { get; }

        private readonly ICollectionView _ticketsView;
        public ICollectionView TicketsView => _ticketsView;

        // =========================================================
        // SELECTION
        // =========================================================

        private RepairTicket _selectedTicket;
        public RepairTicket SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                if (SetProperty(ref _selectedTicket, value))
                {
                    LoadFromTicket(value);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // =========================================================
        // SEARCH & FILTER
        // =========================================================

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                    _ticketsView.Refresh();
            }
        }

        public ObservableCollection<string> StatusFilters { get; } =
            new ObservableCollection<string> { "All", "Open", "Closed" };

        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                    _ticketsView.Refresh();
            }
        }

        public ObservableCollection<string> PriorityFilters { get; } =
            new ObservableCollection<string> { "All", "Minor", "Normal", "Major", "Critical" };

        private string _selectedPriorityFilter = "All";
        public string SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                    _ticketsView.Refresh();
            }
        }

        // =========================================================
        // CURRENCY
        // =========================================================

        public ObservableCollection<string> Currencies { get; } =
            new ObservableCollection<string> { "S.P", "USD", "EUR", "RON", "GBP", "CHF", "CAD" };

        private string _estimatedCostCurrency = "S.P";
        public string EstimatedCostCurrency
        {
            get => _estimatedCostCurrency;
            set => SetProperty(ref _estimatedCostCurrency, value);
        }

        private string _finalCostCurrency = "S.P";
        public string FinalCostCurrency
        {
            get => _finalCostCurrency;
            set => SetProperty(ref _finalCostCurrency, value);
        }

        // =========================================================
        // EDIT BUFFER (FORM STATE)
        // =========================================================

        private string _escTicketId = string.Empty;
        public string EscTicketId { get => _escTicketId; set => SetProperty(ref _escTicketId, value); }

        private string _customerIdText = string.Empty;
        public string CustomerIdText { get => _customerIdText; set => SetProperty(ref _customerIdText, value); }

        private string _customerName = string.Empty;
        public string CustomerName { get => _customerName; set => SetProperty(ref _customerName, value); }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }

        private string _contactMethod = "Call";
        public string ContactMethod { get => _contactMethod; set => SetProperty(ref _contactMethod, value); }

        private string _deviceCategory = string.Empty;
        public string DeviceCategory { get => _deviceCategory; set => SetProperty(ref _deviceCategory, value); }

        private string _deviceBrand = string.Empty;
        public string DeviceBrand { get => _deviceBrand; set => SetProperty(ref _deviceBrand, value); }

        private string _deviceModel = string.Empty;
        public string DeviceModel { get => _deviceModel; set => SetProperty(ref _deviceModel, value); }

        private string _serialIMEI = string.Empty;
        public string SerialIMEI { get => _serialIMEI; set => SetProperty(ref _serialIMEI, value); }

        private string _damageHistory = string.Empty;
        public string DamageHistory { get => _damageHistory; set => SetProperty(ref _damageHistory, value); }

        private string _boardModifications = string.Empty;
        public string BoardModifications { get => _boardModifications; set => SetProperty(ref _boardModifications, value); }

        private string _problemDescription = string.Empty;
        public string ProblemDescription { get => _problemDescription; set => SetProperty(ref _problemDescription, value); }

        private string _notes = string.Empty;
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        private string _repairStatus = "Received";
        public string RepairStatus { get => _repairStatus; set => SetProperty(ref _repairStatus, value); }

        private string _priorityLevel = "Normal";
        public string PriorityLevel { get => _priorityLevel; set => SetProperty(ref _priorityLevel, value); }

        private decimal? _estimatedCost;
        public decimal? EstimatedCost { get => _estimatedCost; set => SetProperty(ref _estimatedCost, value); }

        private decimal? _finalCost;
        public decimal? FinalCost { get => _finalCost; set => SetProperty(ref _finalCost, value); }

        private DateTime _receiveDate = DateTime.Today;
        public DateTime ReceiveDate { get => _receiveDate; set => SetProperty(ref _receiveDate, value); }

        private TimeSpan _receiveTime = DateTime.Now.TimeOfDay;
        public TimeSpan ReceiveTime { get => _receiveTime; set => SetProperty(ref _receiveTime, value); }

        private DateTime? _deliveryDate;
        public DateTime? DeliveryDate { get => _deliveryDate; set => SetProperty(ref _deliveryDate, value); }

        private TimeSpan? _deliveryTime;
        public TimeSpan? DeliveryTime { get => _deliveryTime; set => SetProperty(ref _deliveryTime, value); }

        private string _partsUsed = string.Empty;
        public string PartsUsed
        {
            get => _partsUsed;
            set
            {
                if (SetProperty(ref _partsUsed, value) && !_suppressPartsUsedSync)
                {
                    SyncPartsCollectionFromJson();
                }
            }
        }

        public ObservableCollection<string> SelectedPartsUsed { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> PartsSuggestions { get; } = new ObservableCollection<string>();

        private readonly List<string> _partsCatalog = new List<string>();

        private string _partsUsedInput = string.Empty;
        public string PartsUsedInput
        {
            get => _partsUsedInput;
            set
            {
                if (SetProperty(ref _partsUsedInput, value))
                {
                    UpdatePartsSuggestions();
                }
            }
        }

        private bool _isPartsSuggestionOpen;
        public bool IsPartsSuggestionOpen
        {
            get => _isPartsSuggestionOpen;
            set => SetProperty(ref _isPartsSuggestionOpen, value);
        }

        private bool _suppressPartsUsedSync;

        private string _rootCause = string.Empty;
        public string RootCause { get => _rootCause; set => SetProperty(ref _rootCause, value); }

        private bool _hasWarranty;
        public bool HasWarranty { get => _hasWarranty; set => SetProperty(ref _hasWarranty, value); }

        private string _warrantyPeriod = string.Empty;
        public string WarrantyPeriod { get => _warrantyPeriod; set => SetProperty(ref _warrantyPeriod, value); }

        private bool _isWarrantyRepair;
        public bool IsWarrantyRepair { get => _isWarrantyRepair; set => SetProperty(ref _isWarrantyRepair, value); }

        private bool _isReadyForPickup;
        public bool IsReadyForPickup { get => _isReadyForPickup; set => SetProperty(ref _isReadyForPickup, value); }

        private DeviceChecklist _deviceChecklist = new DeviceChecklist();
        public DeviceChecklist DeviceChecklist { get => _deviceChecklist; set => SetProperty(ref _deviceChecklist, value); }

        private Accessories _accessories = new Accessories();
        public Accessories Accessories { get => _accessories; set => SetProperty(ref _accessories, value); }

        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ReopenCommand { get; }
        public ICommand AddPartCommand { get; }
        public ICommand AddPartFromInputCommand { get; }
        public ICommand RemovePartCommand { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RepairTicketsViewModel()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
            _service = new TicketsDataService(dbPath);

            Tickets = new ObservableCollection<RepairTicket>(_service.GetAll());

            _ticketsView = CollectionViewSource.GetDefaultView(Tickets);
            _ticketsView.Filter = TicketFilter;

            AddCommand = new RelayCommand(_ => AddTicket(), _ => SelectedTicket == null);
            SaveCommand = new RelayCommand(_ => SaveTicket(), _ => SelectedTicket != null);
            DeleteCommand = new RelayCommand(_ => DeleteTicket(), _ => SelectedTicket != null);
            ClearCommand = new RelayCommand(_ => ClearForm());
            CloseCommand = new RelayCommand(_ => CloseTicket(), _ => CanCloseTicket());
            ReopenCommand = new RelayCommand(_ => ReopenTicket(), _ => CanReopenTicket());
            AddPartCommand = new RelayCommand(param => AddPart(param as string));
            AddPartFromInputCommand = new RelayCommand(_ => AddPart(PartsUsedInput));
            RemovePartCommand = new RelayCommand(param => RemovePart(param as string));

            ClearForm();

            LoadPartsCatalog();

            SelectedPartsUsed.CollectionChanged += (_, __) => SyncPartsUsedFromCollection();
        }

        // =========================================================
        // FILTER LOGIC
        // =========================================================

        private bool TicketFilter(object obj)
        {
            if (obj is not RepairTicket t)
                return false;

            if (SelectedStatusFilter == "Open" && t.DeliveryDate.HasValue)
                return false;

            if (SelectedStatusFilter == "Closed" && !t.DeliveryDate.HasValue)
                return false;

            if (SelectedPriorityFilter != "All" &&
                !string.Equals(t.PriorityLevel, SelectedPriorityFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim();

                var match =
                    (t.CustomerName?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.PhoneNumber?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.DeviceModel?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.DeviceCategory?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.SerialIMEI?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.EscTicketId?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!match)
                    return false;
            }

            return true;
        }

        // =========================================================
        // COMMAND ACTIONS
        // =========================================================

        private void AddTicket()
        {
            try
            {
                if (!ValidateForm())
                    return;

                var ticket = BuildTicketFromForm();
                ticket.TicketId = _service.Insert(ticket);

                Tickets.Insert(0, ticket);
                _ticketsView.Refresh();

                SelectedTicket = ticket;
                AppLogger.Success("Ticket added successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to add ticket: {ex.Message}");
            }
        }

        private void SaveTicket()
        {
            if (SelectedTicket == null)
                return;

            try
            {
                if (!ValidateForm())
                    return;

                ApplyFormToTicket(SelectedTicket);
                _service.Update(SelectedTicket);

                RefreshTicketInCollection(SelectedTicket);
                _ticketsView.Refresh();

                AppLogger.Success("Ticket updated successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to save ticket: {ex.Message}");
            }
        }

        private void DeleteTicket()
        {
            if (SelectedTicket == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"Delete ticket '{SelectedTicket.CustomerName}' (ESC-ID: {SelectedTicket.EscTicketId})?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _service.Delete(SelectedTicket.TicketId);
                Tickets.Remove(SelectedTicket);

                ClearForm();
                _ticketsView.Refresh();

                AppLogger.Success("Ticket deleted successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to delete ticket: {ex.Message}");
            }
        }

        // =========================================================
        // CLOSE / REOPEN
        // =========================================================

        private bool CanCloseTicket() =>
            SelectedTicket != null && !SelectedTicket.DeliveryDate.HasValue;

        private void CloseTicket()
        {
            if (!CanCloseTicket())
                return;

            var result = System.Windows.MessageBox.Show(
                $"Close ticket '{SelectedTicket.CustomerName}' (ESC-ID: {SelectedTicket.EscTicketId})?",
                "Confirm Close",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var now = DateTime.Now;

                SelectedTicket.DeliveryDate = now;
                SelectedTicket.RepairStatus = "Completed";

                _service.Update(SelectedTicket);
                RefreshTicketInCollection(SelectedTicket);

                LoadFromTicket(SelectedTicket);
                _ticketsView.Refresh();

                AppLogger.Success("Ticket closed successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to close ticket: {ex.Message}");
            }
        }

        private bool CanReopenTicket() =>
            SelectedTicket != null && SelectedTicket.DeliveryDate.HasValue;

        private void ReopenTicket()
        {
            if (!CanReopenTicket())
                return;

            var result = System.Windows.MessageBox.Show(
                $"Reopen ticket '{SelectedTicket.CustomerName}' (ESC-ID: {SelectedTicket.EscTicketId})?",
                "Confirm Reopen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                SelectedTicket.DeliveryDate = null;
                SelectedTicket.RepairStatus = "In Repair";

                _service.Update(SelectedTicket);
                RefreshTicketInCollection(SelectedTicket);

                LoadFromTicket(SelectedTicket);
                _ticketsView.Refresh();

                AppLogger.Success("Ticket reopened successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to reopen ticket: {ex.Message}");
            }
        }

        // =========================================================
        // FORM ENGINE
        // =========================================================

        private void ClearForm()
        {
            SelectedTicket = null;

            EscTicketId = _service.GetNextEscTicketId();
            CustomerIdText = string.Empty;
            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            ContactMethod = "Call";
            DeviceCategory = string.Empty;
            DeviceBrand = string.Empty;
            DeviceModel = string.Empty;
            SerialIMEI = string.Empty;
            DamageHistory = string.Empty;
            BoardModifications = string.Empty;
            ProblemDescription = string.Empty;
            Notes = string.Empty;
            RepairStatus = "Received";
            PriorityLevel = "Normal";
            EstimatedCost = null;
            FinalCost = null;
            EstimatedCostCurrency = "S.P";
            FinalCostCurrency = "S.P";
            RootCause = string.Empty;
            PartsUsed = string.Empty;
            PartsUsedInput = string.Empty;
            IsPartsSuggestionOpen = false;
            HasWarranty = false;
            WarrantyPeriod = string.Empty;
            IsWarrantyRepair = false;
            IsReadyForPickup = false;

            ReceiveDate = DateTime.Today;
            ReceiveTime = DateTime.Now.TimeOfDay;
            DeliveryDate = null;
            DeliveryTime = null;

            DeviceChecklist = new DeviceChecklist();
            Accessories = new Accessories();
        }

        private void LoadPartsCatalog()
        {
            _partsCatalog.Clear();

            try
            {
                var partsRepo = new PartsRepository();
                var skus = partsRepo.GetSkus()
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                var inventoryRepo = new InventoryRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql"));
                var inventoryNames = inventoryRepo.GetNames()
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                var combined = skus
                    .Concat(inventoryNames)
                    .OrderBy(name => name);

                _partsCatalog.AddRange(combined);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load parts catalog: {ex.Message}");
            }

            UpdatePartsSuggestions();
        }

        private void AddPart(string part)
        {
            var normalized = NormalizePart(part);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (SelectedPartsUsed.Any(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                PartsUsedInput = string.Empty;
                IsPartsSuggestionOpen = false;
                return;
            }

            if (_partsCatalog.All(part => !string.Equals(part, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                _partsCatalog.Add(normalized);
            }

            SelectedPartsUsed.Add(normalized);
            DeductPartFromStock(normalized);
            PartsUsedInput = string.Empty;
            IsPartsSuggestionOpen = false;
        }

        private void DeductPartFromStock(string partName)
        {
            try
            {
                var partsRepo = new PartsRepository();
                partsRepo.DecrementQuantityBySku(partName, 1);

                var inventoryRepo = new InventoryRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql"));
                inventoryRepo.DecrementQuantityByName(partName, 1);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to deduct stock for part '{partName}': {ex.Message}");
            }
        }

        private void RemovePart(string part)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                return;
            }

            var existing = SelectedPartsUsed.FirstOrDefault(p => string.Equals(p, part, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                SelectedPartsUsed.Remove(existing);
            }
        }

        private void UpdatePartsSuggestions()
        {
            PartsSuggestions.Clear();

            var query = PartsUsedInput?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                IsPartsSuggestionOpen = false;
                return;
            }

            var matches = _partsCatalog
                .Where(part => part.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(part => SelectedPartsUsed.All(selected => !string.Equals(selected, part, StringComparison.OrdinalIgnoreCase)))
                .Take(8);

            foreach (var match in matches)
            {
                PartsSuggestions.Add(match);
            }

            IsPartsSuggestionOpen = PartsSuggestions.Count > 0;
        }

        private void SyncPartsUsedFromCollection()
        {
            if (_suppressPartsUsedSync)
            {
                return;
            }

            _suppressPartsUsedSync = true;
            PartsUsed = JsonSerializer.Serialize(SelectedPartsUsed);
            _suppressPartsUsedSync = false;
        }

        private void SyncPartsCollectionFromJson()
        {
            if (_suppressPartsUsedSync)
            {
                return;
            }

            _suppressPartsUsedSync = true;
            SelectedPartsUsed.Clear();
            foreach (var part in ParsePartsUsedJson(PartsUsed))
            {
                SelectedPartsUsed.Add(part);
            }
            _suppressPartsUsedSync = false;
            UpdatePartsSuggestions();
        }

        private static IEnumerable<string> ParsePartsUsedJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<string>();
            }

            try
            {
                var parts = JsonSerializer.Deserialize<List<string>>(json);
                if (parts != null)
                {
                    return parts
                        .Where(part => !string.IsNullOrWhiteSpace(part))
                        .Select(part => part.Trim());
                }
            }
            catch (JsonException)
            {
            }

            return json
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim());
        }

        private static string NormalizePart(string part)
        {
            return string.IsNullOrWhiteSpace(part) ? string.Empty : part.Trim();
        }

        private void LoadFromTicket(RepairTicket ticket)
        {
            if (ticket == null)
            {
                ClearForm();
                return;
            }

            EscTicketId = ticket.EscTicketId;
            CustomerIdText = ticket.CustomerId?.ToString() ?? string.Empty;
            CustomerName = ticket.CustomerName ?? string.Empty;
            PhoneNumber = ticket.PhoneNumber ?? string.Empty;
            ContactMethod = ticket.ContactMethod ?? "Call";
            DeviceCategory = ticket.DeviceCategory ?? string.Empty;
            DeviceBrand = ticket.DeviceBrand ?? string.Empty;
            DeviceModel = ticket.DeviceModel ?? string.Empty;
            SerialIMEI = ticket.SerialIMEI ?? string.Empty;
            DamageHistory = ticket.DamageHistory ?? string.Empty;
            BoardModifications = ticket.BoardModifications ?? string.Empty;
            ProblemDescription = ticket.ProblemDescription ?? string.Empty;
            Notes = ticket.Notes ?? string.Empty;
            RepairStatus = ticket.RepairStatus ?? "Received";
            PriorityLevel = ticket.PriorityLevel ?? "Normal";
            EstimatedCost = ticket.EstimatedCost;
            FinalCost = ticket.FinalCost;
            EstimatedCostCurrency = ticket.EstimatedCostCurrency ?? "S.P";
            FinalCostCurrency = ticket.FinalCostCurrency ?? "S.P";
            RootCause = ticket.RootCause ?? string.Empty;
            PartsUsed = ticket.PartsUsed ?? string.Empty;
            HasWarranty = ticket.HasWarranty;
            WarrantyPeriod = ticket.WarrantyPeriod ?? string.Empty;
            IsWarrantyRepair = ticket.IsWarrantyRepair;
            IsReadyForPickup = ticket.IsReadyForPickup;

            ReceiveDate = ticket.ReceiveDate.Date;
            ReceiveTime = ticket.ReceiveDate.TimeOfDay;

            if (ticket.DeliveryDate.HasValue)
            {
                DeliveryDate = ticket.DeliveryDate.Value.Date;
                DeliveryTime = ticket.DeliveryDate.Value.TimeOfDay;
            }
            else
            {
                DeliveryDate = null;
                DeliveryTime = null;
            }

            DeviceChecklist = ticket.DeviceChecklist ?? new DeviceChecklist();
            Accessories = ticket.Accessories ?? new Accessories();
        }

        private RepairTicket BuildTicketFromForm()
        {
            var receiveDateTime = ReceiveDate.Date.Add(ReceiveTime);

            return new RepairTicket
            {
                EscTicketId = EscTicketId,
                CustomerId = long.TryParse(CustomerIdText, out var id) ? id : null,
                CustomerName = CustomerName,
                PhoneNumber = PhoneNumber,
                ContactMethod = ContactMethod,
                DeviceCategory = DeviceCategory,
                DeviceBrand = DeviceBrand,
                DeviceModel = DeviceModel,
                SerialIMEI = SerialIMEI,
                DamageHistory = DamageHistory,
                BoardModifications = BoardModifications,
                ProblemDescription = ProblemDescription,
                Notes = Notes,
                RepairStatus = RepairStatus,
                PriorityLevel = PriorityLevel,
                EstimatedCost = EstimatedCost,
                EstimatedCostCurrency = EstimatedCostCurrency,
                FinalCost = FinalCost,
                FinalCostCurrency = FinalCostCurrency,
                RootCause = RootCause,
                PartsUsed = PartsUsed,
                HasWarranty = HasWarranty,
                WarrantyPeriod = WarrantyPeriod,
                IsWarrantyRepair = IsWarrantyRepair,
                IsReadyForPickup = IsReadyForPickup,
                ReceiveDate = receiveDateTime,
                DeliveryDate = DeliveryDate.HasValue && DeliveryTime.HasValue
                    ? DeliveryDate.Value.Date.Add(DeliveryTime.Value)
                    : null,
                DeviceChecklist = DeviceChecklist,
                Accessories = Accessories
            };
        }

        private void ApplyFormToTicket(RepairTicket ticket)
        {
            var receiveDateTime = ReceiveDate.Date.Add(ReceiveTime);

            ticket.EscTicketId = EscTicketId;
            ticket.CustomerId = long.TryParse(CustomerIdText, out var id) ? id : null;
            ticket.CustomerName = CustomerName;
            ticket.PhoneNumber = PhoneNumber;
            ticket.ContactMethod = ContactMethod;
            ticket.DeviceCategory = DeviceCategory;
            ticket.DeviceBrand = DeviceBrand;
            ticket.DeviceModel = DeviceModel;
            ticket.SerialIMEI = SerialIMEI;
            ticket.DamageHistory = DamageHistory;
            ticket.BoardModifications = BoardModifications;
            ticket.ProblemDescription = ProblemDescription;
            ticket.Notes = Notes;
            ticket.RepairStatus = RepairStatus;
            ticket.PriorityLevel = PriorityLevel;
            ticket.EstimatedCost = EstimatedCost;
            ticket.EstimatedCostCurrency = EstimatedCostCurrency;
            ticket.FinalCost = FinalCost;
            ticket.FinalCostCurrency = FinalCostCurrency;
            ticket.RootCause = RootCause;
            ticket.PartsUsed = PartsUsed;
            ticket.HasWarranty = HasWarranty;
            ticket.WarrantyPeriod = WarrantyPeriod;
            ticket.IsWarrantyRepair = IsWarrantyRepair;
            ticket.IsReadyForPickup = IsReadyForPickup;
            ticket.ReceiveDate = receiveDateTime;
            ticket.DeliveryDate = DeliveryDate.HasValue && DeliveryTime.HasValue
                ? DeliveryDate.Value.Date.Add(DeliveryTime.Value)
                : null;
            ticket.DeviceChecklist = DeviceChecklist;
            ticket.Accessories = Accessories;
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                AppLogger.Error("Customer Name is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                AppLogger.Error("Phone Number is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContactMethod))
            {
                AppLogger.Error("Contact Method is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DeviceCategory))
            {
                AppLogger.Error("Device Category is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DeviceModel))
            {
                AppLogger.Error("Device Model is required.");
                return false;
            }

            return true;
        }

        // =========================================================
        // COLLECTION HELPERS
        // =========================================================

        private void RefreshTicketInCollection(RepairTicket ticket)
        {
            var index = Tickets.IndexOf(ticket);
            if (index < 0)
                return;

            Tickets.RemoveAt(index);
            Tickets.Insert(index, ticket);
        }
    }
}
