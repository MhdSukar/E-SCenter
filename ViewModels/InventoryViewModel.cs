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

namespace ESCenter.ViewModels
{
    public class InventoryViewModel : ObservableObject
    {
        private readonly InventoryRepository _repository;
        private readonly ICollectionView _inventoryView;

        public ObservableCollection<InventoryItemModel> Items { get; } = new();
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

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public InventoryViewModel()
        {
            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E-SCenter.sql");
            _repository = new InventoryRepository(dbPath);

            _inventoryView = CollectionViewSource.GetDefaultView(Items);
            _inventoryView.Filter = FilterInventory;

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedItem != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem != null);

            Load();
        }

        private void Load()
        {
            Items.Clear();
            foreach (var item in _repository.GetAll())
            {
                Items.Add(item);
            }
            _inventoryView.Refresh();
        }

        private bool FilterInventory(object obj)
        {
            if (obj is not InventoryItemModel item)
                return false;

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
