using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Views;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class PartsControlViewModel : ObservableObject
    {
        private readonly PartsRepository _repo;

        public PartsControlViewModel()
        {
            try
            {
                var isDesignMode = DesignTimeHelper.IsInDesignMode;
                _repo = AppServices.IsInitialized ? AppServices.Get<PartsRepository>() : new PartsRepository();

                Parts = new ObservableCollection<PartModel>();
                PartsView = CollectionViewSource.GetDefaultView(Parts);
                PartsView.Filter = FilterParts;

                AddPartCommand = new RelayCommand(_ => AddPart());
                EditPartCommand = new RelayCommand(_ => EditPart(), _ => SelectedPart != null);
                RemovePartCommand = new RelayCommand(_ => RemovePart(), _ => SelectedPart != null);

                FilterAllCommand = new RelayCommand(_ => SetStockFilter(null));
                FilterLowCommand = new RelayCommand(_ => SetStockFilter("Low"));
                FilterOutCommand = new RelayCommand(_ => SetStockFilter("Out"));
                ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

                if (isDesignMode)
                {
                    Parts.Add(new PartModel { PartId = 1, SKU = "IC-PWR-01", PartCode = "PMIC-IC", PartType = "IC", QuantityOnHand = 4, Price = 12.5, Category = "Power" });
                    Parts.Add(new PartModel { PartId = 2, SKU = "DSP-GLX-22", PartCode = "LCD-S22", PartType = "Display", QuantityOnHand = 1, Price = 85, Category = "Screen" });
                    BuildPartTypes();
                    PartsView.Refresh();
                    return;
                }

                LoadParts();
                BuildPartTypes();
                DatabasePathService.DatabasePathChanged += (_, __) => LoadParts();
                AppLogger.Success("Parts Control Loaded");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to initialize Parts Control: {ex.Message}");
                throw;
            }
        }

        // -------------------------
        // Collections
        // -------------------------

        public ObservableCollection<PartModel> Parts { get; }

        public ICollectionView PartsView { get; }

        // -------------------------
        // Selection
        // -------------------------

        private PartModel _selectedPart;
        public PartModel SelectedPart
        {
            get => _selectedPart;
            set
            {
                SetProperty(ref _selectedPart, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // -------------------------
        // Search / Filters
        // -------------------------

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    try
                    {
                        PartsView.Refresh();
                        OnPropertyChanged(nameof(PartsCountDisplay));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to refresh parts view on search: {ex.Message}");
                    }
                }
            }
        }

        private ObservableCollection<string> _partTypes = new();
        public ObservableCollection<string> PartTypes
        {
            get => _partTypes;
            set => SetProperty(ref _partTypes, value);
        }

        private string _selectedTypeFilter;
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    try
                    {
                        PartsView.Refresh();
                        OnPropertyChanged(nameof(PartsCountDisplay));
                        AppLogger.Info($"Type filter changed to: {value}");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to refresh parts view on type filter: {ex.Message}");
                    }
                }
            }
        }

        private string _stockFilter; // null | "Low" | "Out"

        // -------------------------
        // Commands
        // -------------------------

        public ICommand AddPartCommand { get; }
        public ICommand EditPartCommand { get; }
        public ICommand RemovePartCommand { get; }
        public ICommand FilterAllCommand { get; }
        public ICommand FilterLowCommand { get; }
        public ICommand FilterOutCommand { get; }
        public ICommand ClearSearchCommand { get; }

        // -------------------------
        // Status Bar
        // -------------------------

        public string PartsCountDisplay =>
            $"Showing {PartsView.Cast<object>().Count()} of {Parts.Count} parts";

        // -------------------------
        // Core Logic
        // -------------------------

        private void LoadParts()
        {
            try
            {
                Parts.Clear();
                var parts = _repo.GetAll();
                int loadedCount = 0;

                foreach (var p in parts)
                {
                    try
                    {
                        HookAutoSave(p);
                        Parts.Add(p);
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to load part {p.PartId}: {ex.Message}");
                    }
                }

                PartsView.Refresh();
                OnPropertyChanged(nameof(PartsCountDisplay));
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load parts: {ex.Message}");
            }
        }

        private void HookAutoSave(PartModel part)
        {
            try
            {
                part.PropertyChanged -= Part_PropertyChanged; // Unsubscribe first to avoid duplicates
                part.PropertyChanged += Part_PropertyChanged;
                AppLogger.Info($"Auto-save hooked for part ID: {part.PartId}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to hook auto-save for part {part.PartId}: {ex.Message}");
            }
        }

        private void Part_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not PartModel part)
                return;

            try
            {
                switch (e.PropertyName)
                {
                    case nameof(PartModel.QuantityOnHand):
                    case nameof(PartModel.Price):
                    case nameof(PartModel.LocationShelf):
                    case nameof(PartModel.LocationBin):
                    case nameof(PartModel.QualityGrade):
                    case nameof(PartModel.UnitValue1):
                    case nameof(PartModel.UnitCode1):
                    case nameof(PartModel.UnitValue2):
                    case nameof(PartModel.UnitCode2):
                    case nameof(PartModel.PartCode):
                    case nameof(PartModel.PartType):
                    case nameof(PartModel.SKU):
                    case nameof(PartModel.Category):
                    case nameof(PartModel.Description):
                    case nameof(PartModel.ChipPartNumber):
                        AppLogger.Info($"Auto-saving part {part.PartId}, property changed: {e.PropertyName}");
                        _repo.Update(part);
                        AppLogger.Success($"Part {part.PartId} auto-saved successfully");
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to auto-save part {part.PartId} on property {e.PropertyName}: {ex.Message}");
            }
        }

        private bool FilterParts(object obj)
        {
            try
            {
                if (obj is not PartModel p)
                    return false;

                // Search
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var t = SearchText.ToLowerInvariant();
                    if (!(p.PartCode?.ToLower().Contains(t) == true ||
                          p.SKU?.ToLower().Contains(t) == true ||
                          p.PartType?.ToLower().Contains(t) == true ||
                          p.Category?.ToLower().Contains(t) == true ||
                          p.Description?.ToLower().Contains(t) == true ||
                          p.ChipPartNumber?.ToLower().Contains(t) == true))
                        return false;
                }

                // Type filter
                if (!string.IsNullOrWhiteSpace(SelectedTypeFilter) &&
                    SelectedTypeFilter != "All" &&
                    !string.Equals(p.PartType, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Stock filter
                if (_stockFilter == "Low" && p.QuantityOnHand > 3)
                    return false;

                if (_stockFilter == "Out" && p.QuantityOnHand > 0)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error in filter logic: {ex.Message}");
                return false; // When in doubt, exclude from view
            }
        }

        private void BuildPartTypes()
        {
            try
            {
                AppLogger.Info("Building part types list...");
                PartTypes.Clear();
                PartTypes.Add("All");

                var types = Parts
                    .Select(p => p.PartType)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                foreach (var t in types)
                {
                    PartTypes.Add(t);
                }

                SelectedTypeFilter = "All";
                AppLogger.Info($"Built part types list with {types.Count} unique types");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to build part types: {ex.Message}");
            }
        }

        private void SetStockFilter(string mode)
        {
            try
            {
                _stockFilter = mode;
                PartsView.Refresh();
                OnPropertyChanged(nameof(PartsCountDisplay));
                AppLogger.Info($"Stock filter set to: {mode ?? "All"}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to set stock filter to {mode}: {ex.Message}");
            }
        }

        // -------------------------
        // Commands
        // -------------------------

        private void AddPart()
        {
            try
            {
                AppLogger.Info("Starting add part operation...");
                var part = new PartModel
                {
                    QuantityOnHand = 0,
                    QualityGrade = 3
                };

                var vm = new AddEditPartViewModel(part, false);
                var win = new AddEditPartWindow { DataContext = vm };

                if (win.ShowDialog() != true)
                {
                    AppLogger.Info("Add part operation cancelled by user");
                    return;
                }

                try
                {
                    part.SKU = SkuGenerator.Generate(
                        part.PartType,
                        part.UnitValue1.ToString(),
                        part.UnitCode1,
                        part.UnitValue2.ToString(),
                        part.UnitCode2,
                        part.PartCode,
                        part.ChipPartNumber);
                    AppLogger.Info($"Generated SKU for new part: {part.SKU}");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to generate SKU: {ex.Message}");
                    part.SKU = "ERROR-GEN";
                }

                try
                {
                    part.PartId = _repo.Insert(part);
                    HookAutoSave(part);
                    Parts.Add(part);
                    SelectedPart = part;

                    BuildPartTypes();
                    PartsView.Refresh();
                    OnPropertyChanged(nameof(PartsCountDisplay));

                    AppLogger.Success($"Part added successfully with ID: {part.PartId}, SKU: {part.SKU}");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to insert part into repository: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to add part: {ex.Message}");
            }
        }

        private void EditPart()
        {
            if (SelectedPart == null)
            {
                AppLogger.Warning("Edit part attempted with no selected part");
                return;
            }

            try
            {
                AppLogger.Info($"Starting edit part operation for ID: {SelectedPart.PartId}");

                // Clone to allow cancel
                var clone = Clone(SelectedPart);

                var vm = new AddEditPartViewModel(clone, true);
                var win = new AddEditPartWindow { DataContext = vm };

                if (win.ShowDialog() != true)
                {
                    AppLogger.Info("Edit part operation cancelled by user");
                    return;
                }

                // Copy back
                Copy(clone, SelectedPart);
                _repo.Update(SelectedPart);
                AppLogger.Success($"Part ID {SelectedPart.PartId} updated successfully");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to edit part ID {SelectedPart.PartId}: {ex.Message}");
            }
        }

        private void RemovePart()
        {
            if (SelectedPart == null)
            {
                AppLogger.Warning("Remove part attempted with no selected part");
                return;
            }

            try
            {
                var partId = SelectedPart.PartId;
                var partSku = SelectedPart.SKU;

                AppLogger.Info($"Starting remove part operation for ID: {partId}, SKU: {partSku}");

                _repo.Delete(partId);
                Parts.Remove(SelectedPart);
                SelectedPart = null;

                BuildPartTypes();
                PartsView.Refresh();
                OnPropertyChanged(nameof(PartsCountDisplay));

                AppLogger.Success($"Part ID {partId}, SKU: {partSku} removed successfully");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to remove part ID {SelectedPart.PartId}: {ex.Message}");
            }
        }

        private static PartModel Clone(PartModel p)
        {
            try
            {
                return new PartModel
                {
                    PartId = p.PartId,
                    SKU = p.SKU,
                    PartCode = p.PartCode,
                    PartType = p.PartType,
                    QuantityOnHand = p.QuantityOnHand,
                    QualityGrade = p.QualityGrade,
                    LocationShelf = p.LocationShelf,
                    LocationBin = p.LocationBin,
                    UnitValue1 = p.UnitValue1,
                    UnitCode1 = p.UnitCode1,
                    UnitValue2 = p.UnitValue2,
                    UnitCode2 = p.UnitCode2,
                    ChipPartNumber = p.ChipPartNumber,
                    Category = p.Category,
                    Description = p.Description
                };
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to clone part {p.PartId}: {ex.Message}");
                throw;
            }
        }

        private static void Copy(PartModel src, PartModel dst)
        {
            try
            {
                dst.PartCode = src.PartCode;
                dst.PartType = src.PartType;
                dst.QuantityOnHand = src.QuantityOnHand;
                dst.QualityGrade = src.QualityGrade;
                dst.LocationShelf = src.LocationShelf;
                dst.LocationBin = src.LocationBin;
                dst.UnitValue1 = src.UnitValue1;
                dst.UnitCode1 = src.UnitCode1;
                dst.UnitValue2 = src.UnitValue2;
                dst.UnitCode2 = src.UnitCode2;
                dst.ChipPartNumber = src.ChipPartNumber;
                dst.Category = src.Category;
                dst.Description = src.Description;

                AppLogger.Info($"Copied properties from source part {src.PartId} to destination part {dst.PartId}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to copy part properties from {src.PartId} to {dst.PartId}: {ex.Message}");
                throw;
            }
        }
    }
}
