using System.Collections.ObjectModel;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;

namespace ESCenter.ViewModels
{
    public class AddEditInventoryViewModel : ObservableObject
    {
        public InventoryItemModel Item { get; }

        public bool IsEditMode { get; }

        public ObservableCollection<string> ItemTypes { get; } = new()
        {
            "Screen", "Battery", "Speaker", "Camera", "Transformer", "Flex", "Board", "Adapter", "Other"
        };

        public ObservableCollection<string> Conditions { get; } = new()
        {
            "New", "Pulled", "Refurbished", "Tested", "Dead"
        };

        public ObservableCollection<int> QualityGrades { get; } = new() { 1, 2, 3, 4, 5 };
        public ObservableCollection<string> Currencies { get; } = new() { "S.P", "USD" };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddEditInventoryViewModel()
        {
            Item = new InventoryItemModel
            {
                QuantityOnHand = 1,
                Price = 0,
                PriceCurrency = "S.P",
                QualityGrade = 3,
                Condition = "New"
            };

            SaveCommand = new RelayCommand(w => Close(w, true));
            CancelCommand = new RelayCommand(w => Close(w, false));
        }

        public AddEditInventoryViewModel(InventoryItemModel existing)
        {
            IsEditMode = true;
            Item = new InventoryItemModel
            {
                InventoryId = existing.InventoryId,
                ItemType = existing.ItemType,
                Brand = existing.Brand,
                Model = existing.Model,
                Variant = existing.Variant,
                Compatibility = existing.Compatibility,
                Specs = existing.Specs,
                Size = existing.Size,
                QuantityOnHand = existing.QuantityOnHand,
                Price = existing.Price,
                PriceCurrency = NormalizeCurrency(existing.PriceCurrency),
                Condition = existing.Condition,
                QualityGrade = existing.QualityGrade,
                Source = existing.Source,
                LocationBox = existing.LocationBox,
                Description = existing.Description,
                Notes = existing.Notes,
                Tags = existing.Tags
            };

            SaveCommand = new RelayCommand(w => Close(w, true));
            CancelCommand = new RelayCommand(w => Close(w, false));
        }

        private void Close(object window, bool result)
        {
            if (window is System.Windows.Window win)
            {
                win.DialogResult = result;
                win.Close();
            }
        }

        private static string NormalizeCurrency(string currency)
            => string.Equals(currency, "USD", System.StringComparison.OrdinalIgnoreCase) ? "USD" : "S.P";
    }
}
