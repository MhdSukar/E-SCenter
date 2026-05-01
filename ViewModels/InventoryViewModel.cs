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
    public class InventoryViewModel : ObservableObject, IDisposable
    {
        private readonly InventoryRepository _repository;
        private readonly ICollectionView _inventoryView;

        public ObservableCollection<InventoryItemModel> Items { get; } = new();
        public ObservableCollection<string> PartTypes { get; } = new();
        public ICollectionView InventoryView => _inventoryView;

        private InventoryItemModel _selectedItem;
        public InventoryItemModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private EventHandler? _databasePathChangedHandler;

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _inventoryView.Refresh();
                }
            }
        }


        private string _selectedTypeFilter = "All";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    _inventoryView.Refresh();
                }
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ExportInventoryCsvCommand { get; }

        public InventoryViewModel()
        {
            var isDesignMode = DesignTimeHelper.IsInDesignMode;
            _repository = AppServices.IsInitialized ? AppServices.Get<InventoryRepository>() : new InventoryRepository();
            if (!isDesignMode)
            {
                _databasePathChangedHandler = (_, __) => Load();
                DatabasePathService.DatabasePathChanged += _databasePathChangedHandler;
            }

            _inventoryView = CollectionViewSource.GetDefaultView(Items);
            _inventoryView.Filter = FilterInventory;

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedItem != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem != null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            ExportInventoryCsvCommand = new RelayCommand(_ => ExportInventoryCsv().FireAndForget(nameof(ExportInventoryCsv)));

            if (isDesignMode)
            {
                Items.Add(new InventoryItemModel { ItemType = "Display", Brand = "Samsung", Model = "S21", QuantityOnHand = 5, Price = 38, PriceCurrency = "USD", Condition = "New", Description = "OLED Screen" });
                Items.Add(new InventoryItemModel { ItemType = "Battery", Brand = "Apple", Model = "iPhone 12", QuantityOnHand = 2, Price = 24, PriceCurrency = "USD", Condition = "Refurb", Description = "Li-Ion Pack" });
                BuildPartTypes();
                _inventoryView.Refresh();
            }
            else
            {
                Load();
            }
        }

        private void Load()
        {
            try
            {
                Items.Clear();
                foreach (var item in _repository.GetAll())
                {
                    Items.Add(item);
                }
                BuildPartTypes();
                _inventoryView.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load inventory: {ex.Message}");
            }
        }

        private bool FilterInventory(object obj)
        {
            if (obj is not InventoryItemModel item)
                return false;

            var matchesType = string.IsNullOrWhiteSpace(SelectedTypeFilter)
                || SelectedTypeFilter == "All"
                || string.Equals(item.ItemType, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase);

            if (!matchesType)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return (item.ItemType?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Brand?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Model?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private void Add()
        {
            var vm = new AddEditInventoryViewModel(new InventoryItemModel());
            var win = new ESCenter.Views.AddEditInventoryWindow
            {
                DataContext = vm
            };

            if (win.ShowDialog() == true)
            {
                _repository.Insert(vm.Item);
                Load();
            }
        }

        private void Edit()
        {
            if (SelectedItem == null) return;

            var clone = Clone(SelectedItem);
            var vm = new AddEditInventoryViewModel(clone);
            var win = new ESCenter.Views.AddEditInventoryWindow
            {
                DataContext = vm
            };

            if (win.ShowDialog() == true)
            {
                _repository.Update(vm.Item);
                RestockWizardHelper.ShowIfNeeded(
                    vm.Item.Description ?? vm.Item.ItemType ?? "Unknown Item",
                    "Inventory", vm.Item.QuantityOnHand);
                Load();
            }
        }

        private void Delete()
        {
            if (SelectedItem == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Delete '{SelectedItem.ItemType}'?",
                "Confirm Removal",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _repository.Delete(SelectedItem.InventoryId);
                Load();
            }
        }

        private async System.Threading.Tasks.Task ExportInventoryCsv()
        {
            await CsvExportService.ExportAsync(Items.Cast<object>(),
                new[] { "ItemType", "Brand", "Model", "Qty", "Price", "Currency", "Condition", "Quality", "Source", "Box", "Description" },
                row =>
                {
                    var i = (InventoryItemModel)row;
                    return new[] { i.ItemType ?? string.Empty, i.Brand ?? string.Empty, i.Model ?? string.Empty, i.QuantityOnHand.ToString(), i.Price.ToString("0.##"), i.PriceCurrency ?? "S.P", i.Condition ?? string.Empty, i.QualityGrade.ToString(), i.Source ?? string.Empty, i.LocationBox ?? string.Empty, i.Description ?? string.Empty };
                }, "inventory-export.csv");
        }

        private InventoryItemModel Clone(InventoryItemModel src)
        {
            return new InventoryItemModel
            {
                InventoryId = src.InventoryId,
                ItemType = src.ItemType,
                Brand = src.Brand,
                Model = src.Model,
                Variant = src.Variant,
                Compatibility = src.Compatibility,
                Specs = src.Specs,
                Size = src.Size,
                QuantityOnHand = src.QuantityOnHand,
                Price = src.Price,
                PriceCurrency = src.PriceCurrency,
                Condition = src.Condition,
                QualityGrade = src.QualityGrade,
                Source = src.Source,
                LocationBox = src.LocationBox,
                Description = src.Description,
                Notes = src.Notes,
                Tags = src.Tags
            };
        }

        private void BuildPartTypes()
        {
            PartTypes.Clear();
            PartTypes.Add("All");

            var types = Items
                .Select(i => i.ItemType)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToList();

            foreach (var type in types)
            {
                PartTypes.Add(type);
            }

            if (!PartTypes.Contains(SelectedTypeFilter))
            {
                SelectedTypeFilter = "All";
            }
        }

        public void Dispose()
        {
            if (_databasePathChangedHandler != null)
            {
                DatabasePathService.DatabasePathChanged -= _databasePathChangedHandler;
                _databasePathChangedHandler = null;
            }
        }

    }
}
