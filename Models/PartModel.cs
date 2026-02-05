using System;
using ESCenter.Core; // where ObservableObject lives

namespace ESCenter.Models
{
    public class PartModel : ObservableObject
    {
        private int _partId;
        public int PartId
        {
            get => _partId;
            set => SetProperty(ref _partId, value);
        }

        private string _sku;
        public string SKU
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        private string _partCode;
        public string PartCode
        {
            get => _partCode;
            set => SetProperty(ref _partCode, value);
        }

        private string _partType;
        public string PartType
        {
            get => _partType;
            set => SetProperty(ref _partType, value);
        }

        private int _quantityOnHand;
        public int QuantityOnHand
        {
            get => _quantityOnHand;
            set
            {
                if (SetProperty(ref _quantityOnHand, value))
                    OnPropertyChanged(nameof(StockStatus));
            }
        }

        private double _price;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private int _qualityGrade;
        public int QualityGrade
        {
            get => _qualityGrade;
            set => SetProperty(ref _qualityGrade, value);
        }

        private string _locationShelf;
        public string LocationShelf
        {
            get => _locationShelf;
            set => SetProperty(ref _locationShelf, value);
        }

        private string _locationBin;
        public string LocationBin
        {
            get => _locationBin;
            set => SetProperty(ref _locationBin, value);
        }

        private double _unitValue1;
        public double UnitValue1
        {
            get => _unitValue1;
            set
            {
                if (SetProperty(ref _unitValue1, value))
                    OnPropertyChanged(nameof(Unit1Display));
            }
        }

        private string _unitCode1;
        public string UnitCode1
        {
            get => _unitCode1;
            set
            {
                if (SetProperty(ref _unitCode1, value))
                    OnPropertyChanged(nameof(Unit1Display));
            }
        }

        private double _unitValue2;
        public double UnitValue2
        {
            get => _unitValue2;
            set
            {
                if (SetProperty(ref _unitValue2, value))
                    OnPropertyChanged(nameof(Unit2Display));
            }
        }

        private string _unitCode2;
        public string UnitCode2
        {
            get => _unitCode2;
            set
            {
                if (SetProperty(ref _unitCode2, value))
                    OnPropertyChanged(nameof(Unit2Display));
            }
        }

        private string _chipPartNumber;
        public string ChipPartNumber
        {
            get => _chipPartNumber;
            set => SetProperty(ref _chipPartNumber, value);
        }

        private string _category;
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        // -------------------------
        // Computed UI Properties
        // -------------------------

        public string StockStatus =>
            QuantityOnHand <= 0 ? "Out" :
            QuantityOnHand <= 3 ? "Low" :
            "OK";

        public string Unit1Display =>
            string.IsNullOrWhiteSpace(UnitCode1) ? "" : $"{UnitValue1} {UnitCode1}";

        public string Unit2Display =>
            string.IsNullOrWhiteSpace(UnitCode2) ? "" : $"{UnitValue2} {UnitCode2}";

        public string QualityDisplay => QualityGrade switch
        {
            1 => "Low",
            2 => "Mid",
            3 => "High",
            4 => "Original",
            _ => "Mid"
        };

        public void SetQualityFromDisplay(string value)
        {
            QualityGrade = value switch
            {
                "Low" => 1,
                "Mid" => 2,
                "High" => 3,
                "Original" => 4,
                _ => 2
            };
        }

    }
}
