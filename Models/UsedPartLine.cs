using ESCenter.Core;

namespace ESCenter.Models
{
    public class UsedPartLine : ObservableObject
    {
        public InventoryItemModel Part { get; }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public UsedPartLine(InventoryItemModel part)
        {
            Part = part;
        }
    }
}
