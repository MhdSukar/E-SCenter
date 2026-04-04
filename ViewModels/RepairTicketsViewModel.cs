using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
        // LOADING FLAG
        // =========================================================
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

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

        public ObservableCollection<string> TicketTypeOptions { get; } =
            new ObservableCollection<string> { "Normal", "Basic", "Advanced", "Premium" };

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

        private bool _showReadyPickupsOnly;
        public bool ShowReadyPickupsOnly
        {
            get => _showReadyPickupsOnly;
            set
            {
                if (SetProperty(ref _showReadyPickupsOnly, value))
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
        public string CustomerIdText
        {
            get => _customerIdText;
            set
            {
                if (SetProperty(ref _customerIdText, value))
                    RefreshCustomerProfile();
            }
        }

        private string _customerName = string.Empty;
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                    RefreshCustomerProfile();
            }
        }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                    RefreshCustomerProfile();
            }
        }

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

        private string _ticketType = "Normal";
        public string TicketType { get => _ticketType; set => SetProperty(ref _ticketType, value); }

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

        // ── Parts Used ─────────────────────────────────────────────────────
        private string _partsUsed = string.Empty;
        public string PartsUsed
        {
            get => _partsUsed;
            set
            {
                if (SetProperty(ref _partsUsed, value) && !_suppressPartsSync)
                    SyncLinesFromJson();
            }
        }

        /// <summary>Rich lines shown as chips in the UI (source of truth for editing).</summary>
        public ObservableCollection<UsedPartLine> UsedPartLines { get; } = new();

        /// <summary>Autocomplete suggestions shown in the dropdown.</summary>
        public ObservableCollection<PartSuggestionItem> PartsSuggestions { get; } = new();

        private readonly List<PartSuggestionItem> _catalogItems = new();

        private string _partsUsedInput = string.Empty;
        public string PartsUsedInput
        {
            get => _partsUsedInput;
            set
            {
                if (SetProperty(ref _partsUsedInput, value))
                    UpdatePartsSuggestions();
            }
        }

        private bool _isPartsSuggestionOpen;
        public bool IsPartsSuggestionOpen
        {
            get => _isPartsSuggestionOpen;
            set => SetProperty(ref _isPartsSuggestionOpen, value);
        }

        private int _selectedSuggestionIndex = -1;
        public int SelectedSuggestionIndex
        {
            get => _selectedSuggestionIndex;
            set => SetProperty(ref _selectedSuggestionIndex, value);
        }

        private bool _suppressPartsSync;

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
        public ICommand AddCommand             { get; }
        public ICommand SaveCommand            { get; }
        public ICommand DeleteCommand          { get; }
        public ICommand ClearCommand           { get; }
        public ICommand CloseCommand           { get; }
        public ICommand ReopenCommand          { get; }

        // Parts commands
        public ICommand AddPartCommand            { get; }
        public ICommand AddPartFromInputCommand   { get; }
        public ICommand RemovePartCommand         { get; }
        public ICommand IncrementPartCommand      { get; }
        public ICommand DecrementPartCommand      { get; }
        public ICommand SuggestionMoveDownCommand { get; }
        public ICommand SuggestionMoveUpCommand   { get; }
        public ICommand CloseSuggestionsCommand   { get; }
        public ICommand AcceptSuggestionCommand   { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================
        public RepairTicketsViewModel()
        {
            _service = new TicketsDataService();
            DatabasePathService.DatabasePathChanged += async (_, __) => await LoadTicketsAsync();

            Tickets = new ObservableCollection<RepairTicket>();
            _ = LoadTicketsAsync();

            _ticketsView = CollectionViewSource.GetDefaultView(Tickets);
            _ticketsView.Filter = TicketFilter;

            // Ticket commands
            AddCommand    = new RelayCommand(_ => AddTicket(),    _ => SelectedTicket == null);
            SaveCommand   = new RelayCommand(_ => SaveTicket(),   _ => SelectedTicket != null);
            DeleteCommand = new RelayCommand(_ => DeleteTicket(), _ => SelectedTicket != null);
            ClearCommand  = new RelayCommand(_ => ClearForm());
            CloseCommand  = new RelayCommand(_ => CloseTicket(),  _ => CanCloseTicket());
            ReopenCommand = new RelayCommand(_ => ReopenTicket(), _ => CanReopenTicket());

            // Parts commands
            AddPartCommand = new RelayCommand(param =>
            {
                if (param is PartSuggestionItem suggestion) AddPartFromSuggestion(suggestion);
                else if (param is string name)              AddPartByName(name);
            });
            AddPartFromInputCommand   = new RelayCommand(_ => AddPartFromInput());
            RemovePartCommand         = new RelayCommand(param => RemovePart(param as UsedPartLine));
            IncrementPartCommand      = new RelayCommand(param => IncrementPart(param as UsedPartLine));
            DecrementPartCommand      = new RelayCommand(param => DecrementPart(param as UsedPartLine));
            SuggestionMoveDownCommand = new RelayCommand(_ => MoveSuggestionDown());
            SuggestionMoveUpCommand   = new RelayCommand(_ => MoveSuggestionUp());
            CloseSuggestionsCommand   = new RelayCommand(_ => CloseSuggestions());
            AcceptSuggestionCommand   = new RelayCommand(_ => AcceptSuggestion());

            // Sync UsedPartLines → PartsUsed JSON whenever a line's Quantity changes
            UsedPartLines.CollectionChanged += (_, __) => SyncJsonFromLines();

            ClearForm();
            _ = LoadPartsCatalogAsync();

            SelectedPartsUsed.CollectionChanged += (_, __) => { /* kept for compat, no-op */ };
        }

        // =========================================================
        // COMPAT: keep SelectedPartsUsed as a forwarding alias
        //         so any code not yet migrated doesn't crash
        // =========================================================
        private readonly ObservableCollection<string> _selectedPartsUsed = new();
        public ObservableCollection<string> SelectedPartsUsed => _selectedPartsUsed;

        // =========================================================
        // PUBLIC INTEGRATION HELPERS
        // =========================================================
        public void PrepareNewTicketFromIntegration()
        {
            ShowReadyPickupsOnly = false;
            SearchQuery = string.Empty;
            SelectedStatusFilter = "Open";
            ClearForm();
        }

        public void SearchDeviceFromIntegration(string searchQuery)
        {
            ShowReadyPickupsOnly = false;
            SelectedStatusFilter = "All";
            SearchQuery = searchQuery?.Trim() ?? string.Empty;
        }

        public void ShowReadyPickupsFromIntegration()
        {
            SearchQuery = string.Empty;
            SelectedStatusFilter = "All";
            ShowReadyPickupsOnly = true;
        }

        public ObservableCollection<RepairTicket> CustomerProfileHistory { get; } = new();

        private string _customerProfileHeader = "Customer Profile";
        public string CustomerProfileHeader
        {
            get => _customerProfileHeader;
            private set => SetProperty(ref _customerProfileHeader, value);
        }

        private bool _hasCustomerProfileHistory;
        public bool HasCustomerProfileHistory
        {
            get => _hasCustomerProfileHistory;
            private set => SetProperty(ref _hasCustomerProfileHistory, value);
        }

        private int _customerProfileRepeatCount;
        public int CustomerProfileRepeatCount
        {
            get => _customerProfileRepeatCount;
            set => SetProperty(ref _customerProfileRepeatCount, value);
        }

        // =========================================================
        // TICKET LOADING
        // =========================================================
        private async Task LoadTicketsAsync()
        {
            IsLoading = true;
            try
            {
                var tickets = await _service.GetAllAsync();
                Tickets.Clear();
                foreach (var t in tickets)
                    Tickets.Add(t);
                EvaluateDuplicateMarkers();
                ClearForm();
                RefreshCustomerProfile();
                _ticketsView.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load tickets: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================
        // FILTER
        // =========================================================
        private bool TicketFilter(object obj)
        {
            if (obj is not RepairTicket t) return false;

            if (SelectedStatusFilter == "Open"   && t.DeliveryDate.HasValue)    return false;
            if (SelectedStatusFilter == "Closed" && !t.DeliveryDate.HasValue)   return false;

            if (SelectedPriorityFilter != "All" &&
                !string.Equals(t.PriorityLevel, SelectedPriorityFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (ShowReadyPickupsOnly && !t.IsReadyForPickup) return false;

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
                if (!match) return false;
            }

            return true;
        }

        // =========================================================
        // COMMAND ACTIONS — TICKETS
        // =========================================================
        private void AddTicket()
        {
            try
            {
                if (!ValidateForm()) return;

                var ticket = BuildTicketFromForm();
                var duplicateNotice = BuildDuplicateNotice(ticket);
                ApplyWarrantyRepairSuggestion(ticket);
                ticket.TicketId = _service.Insert(ticket);

                Tickets.Insert(0, ticket);
                EvaluateDuplicateMarkers();

                if (!string.IsNullOrWhiteSpace(duplicateNotice))
                    System.Windows.MessageBox.Show(duplicateNotice, "Repeated Customer / Device",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshCustomerProfile();
                _ticketsView.Refresh();
                ClearForm();

                AppLogger.Success("Ticket added successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
            }
            catch (Exception ex) { AppLogger.Error($"Failed to add ticket: {ex.Message}"); }
        }

        private void SaveTicket()
        {
            if (SelectedTicket == null) return;
            try
            {
                if (!ValidateForm()) return;

                ApplyFormToTicket(SelectedTicket);
                _service.Update(SelectedTicket);

                RefreshTicketInCollection(SelectedTicket);
                EvaluateDuplicateMarkers();
                RefreshCustomerProfile();
                _ticketsView.Refresh();

                AppLogger.Success("Ticket updated successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
            }
            catch (Exception ex) { AppLogger.Error($"Failed to save ticket: {ex.Message}"); }
        }

        private void DeleteTicket()
        {
            if (SelectedTicket == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Delete ticket '{SelectedTicket.CustomerName}' (ESC-ID: {SelectedTicket.EscTicketId})?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                _service.Delete(SelectedTicket.TicketId);
                Tickets.Remove(SelectedTicket);
                EvaluateDuplicateMarkers();
                ClearForm();
                RefreshCustomerProfile();
                _ticketsView.Refresh();

                AppLogger.Success("Ticket deleted successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
            }
            catch (Exception ex) { AppLogger.Error($"Failed to delete ticket: {ex.Message}"); }
        }

        // =========================================================
        // CLOSE / REOPEN
        // =========================================================
        private bool CanCloseTicket()  => SelectedTicket != null && !SelectedTicket.DeliveryDate.HasValue;
        private bool CanReopenTicket() => SelectedTicket != null && SelectedTicket.DeliveryDate.HasValue;

        private void CloseTicket()
        {
            if (!CanCloseTicket()) return;

            var result = System.Windows.MessageBox.Show(
                $"Close ticket '{SelectedTicket.CustomerName}' (ESC-ID: {SelectedTicket.EscTicketId})?",
                "Confirm Close", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                SelectedTicket.DeliveryDate = DateTime.Now;
                SelectedTicket.RepairStatus = "Completed";
                _service.Update(SelectedTicket);
                RefreshTicketInCollection(SelectedTicket);
                LoadFromTicket(SelectedTicket);
                _ticketsView.Refresh();

                AppLogger.Success("Ticket closed successfully!");
                ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
            }
            catch (Exception ex) { AppLogger.Error($"Failed to close ticket: {ex.Message}"); }
        }

        private void ReopenTicket()
        {
            if (!CanReopenTicket()) return;

            var result = System.Windows.MessageBox.Show(
                $"Reopen ticket '{SelectedTicket.CustomerName}' (ESC-ID: {SelectedTicket.EscTicketId})?",
                "Confirm Reopen", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

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
                TicketEvents.RaiseTicketsChanged();
            }
            catch (Exception ex) { AppLogger.Error($"Failed to reopen ticket: {ex.Message}"); }
        }

        // =========================================================
        // PARTS — catalog loading
        // =========================================================
        private void LoadPartsCatalog() => _ = LoadPartsCatalogAsync();

        private async Task LoadPartsCatalogAsync()
        {
            _catalogItems.Clear();
            try
            {
                var partsRepo     = new PartsRepository();
                var inventoryRepo = new InventoryRepository();

                var parts     = await partsRepo.GetAllAsync();
                var inventory = await inventoryRepo.GetAllAsync();

                var partItems = parts
                    .Where(p => !string.IsNullOrWhiteSpace(p.PartCode) || !string.IsNullOrWhiteSpace(p.SKU))
                    .Select(p => new PartSuggestionItem
                    {
                        Name     = (!string.IsNullOrWhiteSpace(p.PartCode) ? p.PartCode : p.SKU)!.Trim(),
                        Sku      = p.SKU?.Trim() ?? string.Empty,
                        StockQty = p.QuantityOnHand,
                        Price    = p.Price,
                        Source   = "Parts"
                    });

                var inventoryItems = inventory
                    .Select(i =>
                    {
                        var name = !string.IsNullOrWhiteSpace(i.Description) ? i.Description.Trim() :
                            string.Join(" ", new[] { i.ItemType, i.Brand, i.Model }
                                .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                        return new PartSuggestionItem
                        {
                            Name     = name,
                            Sku      = string.Empty,
                            StockQty = i.QuantityOnHand,
                            Price    = i.Price,
                            Source   = "Inventory"
                        };
                    })
                    .Where(i => !string.IsNullOrWhiteSpace(i.Name));

                _catalogItems.AddRange(
                    partItems.Concat(inventoryItems)
                        .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.OrderByDescending(x => x.Source == "Parts").First())
                        .OrderBy(x => x.Name));
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load parts catalog: {ex.Message}");
            }

            UpdatePartsSuggestions();
        }

        // =========================================================
        // PARTS — suggestions
        // =========================================================
        private void UpdatePartsSuggestions()
        {
            PartsSuggestions.Clear();
            var query = PartsUsedInput?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                IsPartsSuggestionOpen = false;
                SelectedSuggestionIndex = -1;
                return;
            }

            var matches = _catalogItems
                .Where(c => c.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                // Ranking: prefix match first, then in-stock before out-of-stock, then alpha
                .OrderByDescending(c => c.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(c => c.IsOutOfStock)
                .ThenBy(c => c.IsLowStock ? 1 : 0)
                .ThenBy(c => c.Name)
                .Take(10);

            foreach (var m in matches)
                PartsSuggestions.Add(m);

            IsPartsSuggestionOpen = PartsSuggestions.Count > 0;
            SelectedSuggestionIndex = -1; // user navigates with arrows; -1 = use typed text on Enter
        }

        private void MoveSuggestionDown()
        {
            if (!IsPartsSuggestionOpen || PartsSuggestions.Count == 0) return;
            SelectedSuggestionIndex = Math.Min(SelectedSuggestionIndex + 1, PartsSuggestions.Count - 1);
        }

        private void MoveSuggestionUp()
        {
            if (!IsPartsSuggestionOpen || PartsSuggestions.Count == 0) return;
            SelectedSuggestionIndex = Math.Max(SelectedSuggestionIndex - 1, -1);
        }

        private void CloseSuggestions()
        {
            IsPartsSuggestionOpen = false;
            SelectedSuggestionIndex = -1;
        }

        private void AcceptSuggestion()
        {
            if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < PartsSuggestions.Count)
                AddPartFromSuggestion(PartsSuggestions[SelectedSuggestionIndex]);
            else if (!string.IsNullOrWhiteSpace(PartsUsedInput))
                AddPartByName(PartsUsedInput);
        }

        private void AddPartFromInput()
        {
            if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < PartsSuggestions.Count)
                AddPartFromSuggestion(PartsSuggestions[SelectedSuggestionIndex]);
            else
                AddPartByName(PartsUsedInput);
        }

        // =========================================================
        // PARTS — add / remove / increment / decrement
        // =========================================================

        /// <summary>Add from suggestion (catalog item with stock info).</summary>
        private void AddPartFromSuggestion(PartSuggestionItem suggestion)
        {
            if (suggestion == null) return;

            var existing = UsedPartLines.FirstOrDefault(l =>
                string.Equals(l.Name, suggestion.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Increment existing chip
                existing.Quantity++;
                existing.DeductedQtyInSession++;
                DeductStock(suggestion.Sku, suggestion.Name, 1);
            }
            else
            {
                var line = new UsedPartLine
                {
                    Name                = suggestion.Name,
                    Sku                 = suggestion.Sku,
                    UnitPrice           = suggestion.Price,
                    IsCustom            = false,
                    IsOutOfStock        = suggestion.IsOutOfStock,
                    IsLowStock          = suggestion.IsLowStock,
                    Quantity            = 1,
                    DeductedQtyInSession = 1
                };
                line.PropertyChanged += (_, __) => SyncJsonFromLines();
                UsedPartLines.Add(line);
                DeductStock(suggestion.Sku, suggestion.Name, 1);
            }

            ClearPartsInput();
        }

        /// <summary>Add by name — looks up catalog first; falls back to custom.</summary>
        private void AddPartByName(string name)
        {
            var normalized = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized)) return;

            // Try catalog match first
            var catalogItem = _catalogItems.FirstOrDefault(c =>
                string.Equals(c.Name, normalized, StringComparison.OrdinalIgnoreCase));

            if (catalogItem != null)
            {
                AddPartFromSuggestion(catalogItem);
                return;
            }

            // Custom part not in catalog
            var existing = UsedPartLines.FirstOrDefault(l =>
                string.Equals(l.Name, normalized, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Just increment — custom parts don't have tracked stock
                existing.Quantity++;
            }
            else
            {
                var line = new UsedPartLine
                {
                    Name     = normalized,
                    IsCustom = true,
                    Quantity = 1
                };
                line.PropertyChanged += (_, __) => SyncJsonFromLines();
                UsedPartLines.Add(line);
                // Still attempt stock deduction by name (covers edge case where name = SKU)
                DeductStock(string.Empty, normalized, 1);
            }

            ClearPartsInput();
        }

        private void RemovePart(UsedPartLine line)
        {
            if (line == null) return;
            // Restock whatever was deducted this session
            if (line.DeductedQtyInSession > 0)
                RestoreStock(line.Sku, line.Name, line.DeductedQtyInSession);
            UsedPartLines.Remove(line);
        }

        private void IncrementPart(UsedPartLine line)
        {
            if (line == null) return;
            line.Quantity++;
            if (!line.IsCustom)
            {
                line.DeductedQtyInSession++;
                DeductStock(line.Sku, line.Name, 1);
            }
        }

        private void DecrementPart(UsedPartLine line)
        {
            if (line == null) return;
            if (line.Quantity <= 1)
            {
                RemovePart(line);
                return;
            }
            line.Quantity--;
            if (!line.IsCustom && line.DeductedQtyInSession > 0)
            {
                line.DeductedQtyInSession--;
                RestoreStock(line.Sku, line.Name, 1);
            }
        }

        private void ClearPartsInput()
        {
            PartsUsedInput = string.Empty;
            IsPartsSuggestionOpen = false;
            SelectedSuggestionIndex = -1;
        }

        // =========================================================
        // PARTS — stock management
        // =========================================================
        private void DeductStock(string sku, string name, int qty)
        {
            if (qty <= 0) return;
            try
            {
                var partsRepo = new PartsRepository();
                if (!string.IsNullOrWhiteSpace(sku))
                    partsRepo.DecrementQuantityBySku(sku, qty);
                else if (!string.IsNullOrWhiteSpace(name))
                    partsRepo.DecrementQuantityBySku(name, qty); // try name-as-SKU

                var inventoryRepo = new InventoryRepository();
                inventoryRepo.DecrementQuantityByName(name, qty);
            }
            catch (Exception ex) { AppLogger.Error($"Failed to deduct stock for '{name}': {ex.Message}"); }
        }

        private void RestoreStock(string sku, string name, int qty)
        {
            if (qty <= 0) return;
            try
            {
                var partsRepo = new PartsRepository();
                if (!string.IsNullOrWhiteSpace(sku))
                    partsRepo.RestoreQuantityBySku(sku, qty);
                else if (!string.IsNullOrWhiteSpace(name))
                    partsRepo.RestoreQuantityBySku(name, qty);

                var inventoryRepo = new InventoryRepository();
                inventoryRepo.RestoreQuantityByName(name, qty);
            }
            catch (Exception ex) { AppLogger.Error($"Failed to restore stock for '{name}': {ex.Message}"); }
        }

        // =========================================================
        // PARTS — JSON serialization (bidirectional sync)
        // =========================================================

        /// <summary>Compact DTO for serialization — kept internal to this class.</summary>
        private class PartDto
        {
            [JsonPropertyName("n")] public string Name     { get; set; } = string.Empty;
            [JsonPropertyName("s")] public string Sku      { get; set; } = string.Empty;
            [JsonPropertyName("q")] public int    Qty      { get; set; } = 1;
            [JsonPropertyName("p")] public double Price    { get; set; }
            [JsonPropertyName("c")] public bool   Custom   { get; set; }
        }

        private void SyncJsonFromLines()
        {
            if (_suppressPartsSync) return;
            _suppressPartsSync = true;
            try
            {
                var dtos = UsedPartLines.Select(l => new PartDto
                {
                    Name  = l.Name,
                    Sku   = l.Sku,
                    Qty   = l.Quantity,
                    Price = l.UnitPrice,
                    Custom = l.IsCustom
                }).ToList();
                PartsUsed = dtos.Count > 0
                    ? JsonSerializer.Serialize(dtos)
                    : string.Empty;
            }
            finally { _suppressPartsSync = false; }
        }

        private void SyncLinesFromJson()
        {
            if (_suppressPartsSync) return;
            _suppressPartsSync = true;
            try
            {
                UsedPartLines.Clear();
                var lines = ParsePartsJson(PartsUsed);
                foreach (var line in lines)
                {
                    // Resolve live stock info from catalog
                    var catalogItem = _catalogItems.FirstOrDefault(c =>
                        string.Equals(c.Name, line.Name, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(line.Sku) &&
                         string.Equals(c.Sku, line.Sku, StringComparison.OrdinalIgnoreCase)));

                    if (catalogItem != null)
                    {
                        line.IsOutOfStock = catalogItem.IsOutOfStock;
                        line.IsLowStock   = catalogItem.IsLowStock;
                        line.IsCustom     = false;
                    }

                    line.PropertyChanged += (_, __) => SyncJsonFromLines();
                    UsedPartLines.Add(line);
                }
            }
            finally { _suppressPartsSync = false; }
        }

        private List<UsedPartLine> ParsePartsJson(string json)
        {
            var result = new List<UsedPartLine>();
            if (string.IsNullOrWhiteSpace(json)) return result;

            // Try new DTO format: [{n,s,q,p,c}]
            try
            {
                var dtos = JsonSerializer.Deserialize<List<PartDto>>(json);
                if (dtos != null && dtos.Count > 0 && dtos[0].Name != null)
                {
                    result.AddRange(dtos.Select(d => new UsedPartLine
                    {
                        Name      = d.Name,
                        Sku       = d.Sku,
                        Quantity  = Math.Max(1, d.Qty),
                        UnitPrice = d.Price,
                        IsCustom  = d.Custom
                        // DeductedQtyInSession stays 0 — loaded from DB, not a fresh deduction
                    }));
                    return result;
                }
            }
            catch { /* fall through */ }

            // Backward compat: old format ["part1","part1","part2"] — group duplicates
            try
            {
                var strings = JsonSerializer.Deserialize<List<string>>(json);
                if (strings != null)
                {
                    result.AddRange(
                        strings
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .GroupBy(s => s.Trim(), StringComparer.OrdinalIgnoreCase)
                            .Select(g => new UsedPartLine
                            {
                                Name     = g.Key,
                                Quantity = g.Count(),
                                IsCustom = !_catalogItems.Any(c =>
                                    string.Equals(c.Name, g.Key, StringComparison.OrdinalIgnoreCase))
                            }));
                    return result;
                }
            }
            catch { /* fall through */ }

            // Final fallback: comma/semicolon separated plain text
            result.AddRange(
                json.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => new UsedPartLine { Name = p, IsCustom = true }));

            return result;
        }

        // =========================================================
        // FORM ENGINE
        // =========================================================
        private void ClearForm()
        {
            SelectedTicket = null;

            EscTicketId        = _service.GetNextEscTicketId();
            CustomerIdText     = string.Empty;
            CustomerName       = string.Empty;
            PhoneNumber        = string.Empty;
            ContactMethod      = "Call";
            DeviceCategory     = string.Empty;
            DeviceBrand        = string.Empty;
            DeviceModel        = string.Empty;
            SerialIMEI         = string.Empty;
            DamageHistory      = string.Empty;
            BoardModifications = string.Empty;
            ProblemDescription = string.Empty;
            Notes              = string.Empty;
            RepairStatus       = "Received";
            PriorityLevel      = "Normal";
            TicketType         = "Normal";
            EstimatedCost      = null;
            FinalCost          = null;
            EstimatedCostCurrency = "S.P";
            FinalCostCurrency     = "S.P";
            RootCause          = string.Empty;
            HasWarranty        = false;
            WarrantyPeriod     = string.Empty;
            IsWarrantyRepair   = false;
            IsReadyForPickup   = false;

            ReceiveDate  = DateTime.Today;
            ReceiveTime  = DateTime.Now.TimeOfDay;
            DeliveryDate = null;
            DeliveryTime = null;

            DeviceChecklist = new DeviceChecklist();
            Accessories     = new Accessories();

            // Clear parts (suppress sync to avoid empty JSON write)
            _suppressPartsSync = true;
            UsedPartLines.Clear();
            _suppressPartsSync = false;
            _partsUsed = string.Empty;

            PartsUsedInput          = string.Empty;
            IsPartsSuggestionOpen   = false;
            SelectedSuggestionIndex = -1;

            RefreshCustomerProfile();
        }

        private void LoadFromTicket(RepairTicket ticket)
        {
            if (ticket == null) { ClearForm(); return; }

            EscTicketId        = ticket.EscTicketId;
            CustomerIdText     = ticket.CustomerId?.ToString() ?? string.Empty;
            CustomerName       = ticket.CustomerName ?? string.Empty;
            PhoneNumber        = ticket.PhoneNumber  ?? string.Empty;
            ContactMethod      = ticket.ContactMethod ?? "Call";
            DeviceCategory     = ticket.DeviceCategory ?? string.Empty;
            DeviceBrand        = ticket.DeviceBrand    ?? string.Empty;
            DeviceModel        = ticket.DeviceModel    ?? string.Empty;
            SerialIMEI         = ticket.SerialIMEI     ?? string.Empty;
            DamageHistory      = ticket.DamageHistory  ?? string.Empty;
            BoardModifications = ticket.BoardModifications ?? string.Empty;
            ProblemDescription = ticket.ProblemDescription ?? string.Empty;
            Notes              = ticket.Notes ?? string.Empty;
            RepairStatus       = ticket.RepairStatus  ?? "Received";
            PriorityLevel      = ticket.PriorityLevel ?? "Normal";
            TicketType         = ticket.TicketType    ?? "Normal";
            EstimatedCost         = ticket.EstimatedCost;
            FinalCost             = ticket.FinalCost;
            EstimatedCostCurrency = ticket.EstimatedCostCurrency ?? "S.P";
            FinalCostCurrency     = ticket.FinalCostCurrency     ?? "S.P";
            RootCause        = ticket.RootCause     ?? string.Empty;
            HasWarranty      = ticket.HasWarranty;
            WarrantyPeriod   = ticket.WarrantyPeriod ?? string.Empty;
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
            Accessories     = ticket.Accessories     ?? new Accessories();

            // Load parts — setter triggers SyncLinesFromJson
            PartsUsed = ticket.PartsUsed ?? string.Empty;

            PartsUsedInput          = string.Empty;
            IsPartsSuggestionOpen   = false;
            SelectedSuggestionIndex = -1;

            RefreshCustomerProfile();
        }

        private RepairTicket BuildTicketFromForm()
        {
            var receiveDateTime = ReceiveDate.Date.Add(ReceiveTime);
            return new RepairTicket
            {
                EscTicketId        = EscTicketId,
                CustomerId         = long.TryParse(CustomerIdText, out var id) ? id : null,
                CustomerName       = CustomerName,
                PhoneNumber        = PhoneNumber,
                ContactMethod      = ContactMethod,
                DeviceCategory     = DeviceCategory,
                DeviceBrand        = DeviceBrand,
                DeviceModel        = DeviceModel,
                SerialIMEI         = SerialIMEI,
                DamageHistory      = DamageHistory,
                BoardModifications = BoardModifications,
                ProblemDescription = ProblemDescription,
                Notes              = Notes,
                RepairStatus       = RepairStatus,
                PriorityLevel      = PriorityLevel,
                TicketType         = TicketType,
                EstimatedCost         = EstimatedCost,
                EstimatedCostCurrency = EstimatedCostCurrency,
                FinalCost             = FinalCost,
                FinalCostCurrency     = FinalCostCurrency,
                RootCause        = RootCause,
                PartsUsed        = PartsUsed,
                HasWarranty      = HasWarranty,
                WarrantyPeriod   = WarrantyPeriod,
                IsWarrantyRepair = IsWarrantyRepair,
                IsReadyForPickup = IsReadyForPickup,
                ReceiveDate  = receiveDateTime,
                DeliveryDate = DeliveryDate.HasValue && DeliveryTime.HasValue
                    ? DeliveryDate.Value.Date.Add(DeliveryTime.Value) : null,
                DeviceChecklist = DeviceChecklist,
                Accessories     = Accessories
            };
        }

        private void ApplyFormToTicket(RepairTicket ticket)
        {
            var receiveDateTime = ReceiveDate.Date.Add(ReceiveTime);
            ticket.EscTicketId        = EscTicketId;
            ticket.CustomerId         = long.TryParse(CustomerIdText, out var id) ? id : null;
            ticket.CustomerName       = CustomerName;
            ticket.PhoneNumber        = PhoneNumber;
            ticket.ContactMethod      = ContactMethod;
            ticket.DeviceCategory     = DeviceCategory;
            ticket.DeviceBrand        = DeviceBrand;
            ticket.DeviceModel        = DeviceModel;
            ticket.SerialIMEI         = SerialIMEI;
            ticket.DamageHistory      = DamageHistory;
            ticket.BoardModifications = BoardModifications;
            ticket.ProblemDescription = ProblemDescription;
            ticket.Notes              = Notes;
            ticket.RepairStatus       = RepairStatus;
            ticket.PriorityLevel      = PriorityLevel;
            ticket.TicketType         = TicketType;
            ticket.EstimatedCost         = EstimatedCost;
            ticket.EstimatedCostCurrency = EstimatedCostCurrency;
            ticket.FinalCost             = FinalCost;
            ticket.FinalCostCurrency     = FinalCostCurrency;
            ticket.RootCause        = RootCause;
            ticket.PartsUsed        = PartsUsed;
            ticket.HasWarranty      = HasWarranty;
            ticket.WarrantyPeriod   = WarrantyPeriod;
            ticket.IsWarrantyRepair = IsWarrantyRepair;
            ticket.IsReadyForPickup = IsReadyForPickup;
            ticket.ReceiveDate  = receiveDateTime;
            ticket.DeliveryDate = DeliveryDate.HasValue && DeliveryTime.HasValue
                ? DeliveryDate.Value.Date.Add(DeliveryTime.Value) : null;
            ticket.DeviceChecklist = DeviceChecklist;
            ticket.Accessories     = Accessories;
        }

        private void ApplyWarrantyRepairSuggestion(RepairTicket ticket)
        {
            if (ticket == null) return;
            var match = FindActiveWarrantyMatch(ticket);
            if (match == null) return;

            var result = System.Windows.MessageBox.Show(
                $"This device has an active warranty from ticket {match.EscTicketId}.\nMark this as a warranty repair?",
                "Active Warranty Found", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            ticket.IsWarrantyRepair = true;
            IsWarrantyRepair = true;
        }

        private RepairTicket FindActiveWarrantyMatch(RepairTicket candidate)
        {
            if (candidate == null || !candidate.CustomerId.HasValue ||
                string.IsNullOrWhiteSpace(candidate.SerialIMEI))
                return null;

            var normalizedSerial = candidate.SerialIMEI.Trim();
            return Tickets.FirstOrDefault(existing =>
                existing.CustomerId.HasValue &&
                existing.CustomerId.Value == candidate.CustomerId.Value &&
                !string.IsNullOrWhiteSpace(existing.SerialIMEI) &&
                string.Equals(existing.SerialIMEI.Trim(), normalizedSerial, StringComparison.OrdinalIgnoreCase) &&
                WarrantyEvaluator.HasActiveWarranty(existing));
        }

        private string BuildDuplicateNotice(RepairTicket candidate)
        {
            var repeatedCustomer = Tickets.Any(e =>
                (candidate.CustomerId.HasValue && e.CustomerId.HasValue && candidate.CustomerId.Value == e.CustomerId.Value) ||
                (!string.IsNullOrWhiteSpace(candidate.CustomerName) && !string.IsNullOrWhiteSpace(e.CustomerName) &&
                 string.Equals(candidate.CustomerName.Trim(), e.CustomerName.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(candidate.PhoneNumber) && !string.IsNullOrWhiteSpace(e.PhoneNumber) &&
                 string.Equals(candidate.PhoneNumber.Trim(), e.PhoneNumber.Trim(), StringComparison.OrdinalIgnoreCase)));

            var repeatedDevice = !string.IsNullOrWhiteSpace(candidate.SerialIMEI) &&
                Tickets.Any(e => !string.IsNullOrWhiteSpace(e.SerialIMEI) &&
                    string.Equals(candidate.SerialIMEI.Trim(), e.SerialIMEI.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!repeatedCustomer && !repeatedDevice) return string.Empty;

            var lines = new List<string> { "This ticket matches existing records:" };
            if (repeatedCustomer) lines.Add("• Repeated customer (ID, Name, or Phone matched).");
            if (repeatedDevice)   lines.Add("• Repeated device (Serial/IMEI matched).");
            return string.Join(Environment.NewLine, lines);
        }

        private void EvaluateDuplicateMarkers()
        {
            foreach (var ticket in Tickets)
            {
                ticket.IsRepeatedCustomer =
                    (ticket.CustomerId.HasValue && Tickets.Any(o => o.TicketId != ticket.TicketId && o.CustomerId.HasValue && o.CustomerId.Value == ticket.CustomerId.Value)) ||
                    (!string.IsNullOrWhiteSpace(ticket.CustomerName) && Tickets.Any(o => o.TicketId != ticket.TicketId && !string.IsNullOrWhiteSpace(o.CustomerName) && string.Equals(ticket.CustomerName.Trim(), o.CustomerName.Trim(), StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrWhiteSpace(ticket.PhoneNumber) && Tickets.Any(o => o.TicketId != ticket.TicketId && !string.IsNullOrWhiteSpace(o.PhoneNumber) && string.Equals(ticket.PhoneNumber.Trim(), o.PhoneNumber.Trim(), StringComparison.OrdinalIgnoreCase)));

                ticket.IsRepeatedDevice =
                    !string.IsNullOrWhiteSpace(ticket.SerialIMEI) &&
                    Tickets.Any(o => o.TicketId != ticket.TicketId && !string.IsNullOrWhiteSpace(o.SerialIMEI) &&
                        string.Equals(ticket.SerialIMEI.Trim(), o.SerialIMEI.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        private void RefreshCustomerProfile()
        {
            var customerId     = long.TryParse(CustomerIdText, out var pid) ? pid : (long?)null;
            var normalizedName = NormalizeLookup(CustomerName);
            var normalizedPhone = NormalizeLookup(PhoneNumber);

            var matches = Tickets
                .Where(t => IsCustomerMatch(t, customerId, normalizedName, normalizedPhone))
                .Where(t => !string.Equals(t.RepairStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.ReceiveDate)
                .ToList();

            CustomerProfileHistory.Clear();
            foreach (var m in matches) CustomerProfileHistory.Add(m);

            CustomerProfileRepeatCount  = CustomerProfileHistory.Count;
            HasCustomerProfileHistory   = CustomerProfileRepeatCount > 0;
            CustomerProfileHeader = BuildCustomerProfileHeader(customerId, normalizedName, normalizedPhone, CustomerProfileRepeatCount);
        }

        private static bool IsCustomerMatch(RepairTicket ticket, long? customerId, string name, string phone)
        {
            var idMatch    = customerId.HasValue && ticket.CustomerId.HasValue && ticket.CustomerId.Value == customerId.Value;
            var nameMatch  = !string.IsNullOrWhiteSpace(name)  && string.Equals(NormalizeLookup(ticket.CustomerName), name,  StringComparison.OrdinalIgnoreCase);
            var phoneMatch = !string.IsNullOrWhiteSpace(phone) && string.Equals(NormalizeLookup(ticket.PhoneNumber),  phone, StringComparison.OrdinalIgnoreCase);
            return idMatch || nameMatch || phoneMatch;
        }

        private static string NormalizeLookup(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string BuildCustomerProfileHeader(long? customerId, string name, string phone, int count)
        {
            if (!customerId.HasValue && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone))
                return "Customer Profile";
            return count == 0 ? "Customer Profile - New Customer" : $"Customer Profile - {count} repair record(s)";
        }

        // =========================================================
        // VALIDATION
        // =========================================================
        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))   { AppLogger.Error("Customer Name is required.");   return false; }
            if (string.IsNullOrWhiteSpace(PhoneNumber))    { AppLogger.Error("Phone Number is required.");    return false; }
            if (string.IsNullOrWhiteSpace(ContactMethod))  { AppLogger.Error("Contact Method is required.");  return false; }
            if (string.IsNullOrWhiteSpace(DeviceCategory)) { AppLogger.Error("Device Category is required."); return false; }
            if (string.IsNullOrWhiteSpace(DeviceModel))    { AppLogger.Error("Device Model is required.");    return false; }
            return true;
        }

        // =========================================================
        // COLLECTION HELPERS
        // =========================================================
        private void RefreshTicketInCollection(RepairTicket ticket)
        {
            var index = Tickets.IndexOf(ticket);
            if (index < 0) return;
            Tickets.RemoveAt(index);
            Tickets.Insert(index, ticket);
        }
    }
}
