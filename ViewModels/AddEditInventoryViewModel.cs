using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;

namespace ESCenter.ViewModels
{
    public class AddEditInventoryViewModel : ObservableObject
    {
        private readonly TypeDefinitionsRepository _typeRepo = new();
        public InventoryItemModel Item { get; }
        public bool IsEditMode { get; }
        public ObservableCollection<string> ItemTypes { get; } = new();
        public ObservableCollection<string> Conditions { get; } = new() { "New", "Pulled", "Refurbished", "Tested", "Dead" };
        public ObservableCollection<int> QualityGrades { get; } = new() { 1, 2, 3, 4, 5 };
        public ObservableCollection<string> Currencies { get; } = new() { "S.P", "USD" };
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddNewTypeCommand { get; }
        public bool CanAddNewType => string.Equals(Item?.ItemType, "Other", StringComparison.OrdinalIgnoreCase);

        public AddEditInventoryViewModel() : this(new InventoryItemModel { QuantityOnHand = 1, Price = 0, PriceCurrency = "S.P", QualityGrade = 3, Condition = "New" }, false) { }
        public AddEditInventoryViewModel(InventoryItemModel existing) : this(existing, true) { }
        private AddEditInventoryViewModel(InventoryItemModel existing, bool isEdit)
        {
            IsEditMode = isEdit;
            Item = isEdit ? new InventoryItemModel
            {
                InventoryId = existing.InventoryId, ItemType = existing.ItemType, Brand = existing.Brand, Model = existing.Model, Variant = existing.Variant, Compatibility = existing.Compatibility, Specs = existing.Specs, Size = existing.Size,
                QuantityOnHand = existing.QuantityOnHand, Price = existing.Price, PriceCurrency = NormalizeCurrency(existing.PriceCurrency), Condition = existing.Condition, QualityGrade = existing.QualityGrade,
                Source = existing.Source, LocationBox = existing.LocationBox, Description = existing.Description, Notes = existing.Notes, Tags = existing.Tags
            } : existing;
            LoadTypes();
            SaveCommand = new RelayCommand(w => Close(w, true));
            CancelCommand = new RelayCommand(w => Close(w, false));
            AddNewTypeCommand = new RelayCommand(_ => AddType(), _ => CanAddNewType);
            Item.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Item.ItemType)) OnPropertyChanged(nameof(CanAddNewType)); };
        }
        private void LoadTypes()
        {
            var defaults = new[] { "Screen", "Battery", "Speaker", "Camera", "Transformer", "Flex", "Board", "Adapter" };
            foreach (var t in defaults.Concat(_typeRepo.GetTypes("Inventory")).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x)) ItemTypes.Add(t);
            ItemTypes.Add("Other");
        }
        private void AddType()
        {
            var win = new Views.AddTypeDefinitionWindow(false) { Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault() };
            if (win.ShowDialog() == true)
            {
                _typeRepo.UpsertType("Inventory", win.TypeNameValue, string.Empty, string.Empty);
                ItemTypes.Insert(Math.Max(0, ItemTypes.Count - 1), win.TypeNameValue);
                Item.ItemType = win.TypeNameValue;
            }
        }
        private void Close(object window, bool result) { if (window is Window win) { win.DialogResult = result; win.Close(); } }
        private static string NormalizeCurrency(string currency) => string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "S.P";
    }
}
