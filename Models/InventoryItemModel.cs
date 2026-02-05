using ESCenter.Core;

namespace ESCenter.Models
{
    public class InventoryItemModel : ObservableObject
    {
        private int _inventoryId;
        public int InventoryId
        {
            get => _inventoryId;
            set => SetProperty(ref _inventoryId, value);
        }

        private string _itemType;
        public string ItemType
        {
            get => _itemType;
            set => SetProperty(ref _itemType, value);
        }

        private string _brand;
        public string Brand
        {
            get => _brand;
            set => SetProperty(ref _brand, value);
        }

        private string _model;
        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        private string _variant;
        public string Variant
        {
            get => _variant;
            set => SetProperty(ref _variant, value);
        }

        private string _compatibility;
        public string Compatibility
        {
            get => _compatibility;
            set => SetProperty(ref _compatibility, value);
        }

        private string _specs;
        public string Specs
        {
            get => _specs;
            set => SetProperty(ref _specs, value);
        }

        private string _size;
        public string Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        private int _quantityOnHand;
        public int QuantityOnHand
        {
            get => _quantityOnHand;
            set => SetProperty(ref _quantityOnHand, value);
        }

        private double _price;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private string _condition;
        public string Condition
        {
            get => _condition;
            set => SetProperty(ref _condition, value);
        }

        private int _qualityGrade;
        public int QualityGrade
        {
            get => _qualityGrade;
            set => SetProperty(ref _qualityGrade, value);
        }

        private string _source;
        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private string _locationBox;
        public string LocationBox
        {
            get => _locationBox;
            set => SetProperty(ref _locationBox, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private string _tags;
        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }
    }
}
