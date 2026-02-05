using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;

namespace ESCenter.ViewModels
{
    public class InventoryViewModel : ObservableObject
    {
        private readonly InventoryRepository _repository;

        public ObservableCollection<InventoryItemModel> Items { get; } = new();

        private ICollectionView _inventoryView;
        public ICollectionView InventoryView => _inventoryView;

        private InventoryItemModel _selectedItem;
        public InventoryItemModel SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand RemoveCommand { get; }
        public RelayCommand RefreshCommand { get; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    _inventoryView.Refresh();
            }
        }

        private string _selectedTypeFilter;
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                    _inventoryView.Refresh();
            }
        }

        public ObservableCollection<string> PartTypes { get; } = new();

        public InventoryViewModel()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
            _repository = new InventoryRepository(dbPath);

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedItem != null);
            RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedItem != null);
            RefreshCommand = new RelayCommand(_ => Load());

            _inventoryView = CollectionViewSource.GetDefaultView(Items);
            _inventoryView.Filter = FilterInventory;

            Load();
        }

        private void Load()
        {
            Items.Clear();

            var items = _repository.GetAll().ToList();
            foreach (var item in items)
                Items.Add(item);

            // Build filter list
            PartTypes.Clear();
            PartTypes.Add("All");
            foreach (var t in items.Select(i => i.ItemType).Distinct().OrderBy(x => x))
                PartTypes.Add(t);

            SelectedTypeFilter = "All";
            _inventoryView.Refresh();
        }

        private bool FilterInventory(object obj)
        {
            if (obj is not InventoryItemModel item)
                return false;

            // Type filter
            if (!string.IsNullOrWhiteSpace(SelectedTypeFilter) &&
                SelectedTypeFilter != "All" &&
                !string.Equals(item.ItemType, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLowerInvariant();
                return
                    (item.Brand?.ToLower().Contains(s) ?? false) ||
                    (item.Model?.ToLower().Contains(s) ?? false) ||
                    (item.Variant?.ToLower().Contains(s) ?? false) ||
                    (item.Compatibility?.ToLower().Contains(s) ?? false) ||
                    (item.Description?.ToLower().Contains(s) ?? false) ||
                    (item.Tags?.ToLower().Contains(s) ?? false);
            }

            return true;
        }

        private void Add()
        {
            var vm = new AddEditInventoryViewModel(new InventoryItemModel());
            var win = new ESCenter.Views.AddEditInventoryWindow
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
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
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (win.ShowDialog() == true)
            {
                _repository.Update(vm.Item);
                Load();
            }
        }

        private void Remove()
        {
            if (SelectedItem == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to remove '{SelectedItem.Model}'?",
                "Confirm Removal",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _repository.Delete(SelectedItem.InventoryId);
                Load();
            }
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
                Condition = src.Condition,
                QualityGrade = src.QualityGrade,
                Source = src.Source,
                LocationBox = src.LocationBox,
                Description = src.Description,
                Notes = src.Notes,
                Tags = src.Tags
            };
        }
    }
}
