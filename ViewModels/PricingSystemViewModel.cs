using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public sealed class PricingCostOptionViewModel : ObservableObject
    {
        private string _label = string.Empty;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        private bool _isIncluded;
        public bool IsIncluded
        {
            get => _isIncluded;
            set => SetProperty(ref _isIncluded, value);
        }

        private decimal _value;
        public decimal Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _valueType = PricingService.FixedValueType;
        public string ValueType
        {
            get => _valueType;
            set => SetProperty(ref _valueType, PricingService.NormalizeValueType(value));
        }

        public PricingCostOption ToModel() => new()
        {
            Label = Label,
            IsIncluded = IsIncluded,
            Value = Value,
            ValueType = ValueType
        };

        public static PricingCostOptionViewModel FromModel(PricingCostOption model) => new()
        {
            Label = model.Label,
            IsIncluded = model.IsIncluded,
            Value = model.Value,
            ValueType = PricingService.NormalizeValueType(model.ValueType)
        };
    }

    public sealed class PricingSystemViewModel : ObservableObject
    {
        private string _usdRateText = string.Empty;
        public string UsdRateText
        {
            get => _usdRateText;
            set => SetProperty(ref _usdRateText, value);
        }

        private PricingSettings _settings = PricingService.CreateDefaultSettings();

        public bool IncludeDiagnosticFee { get => _settings.IncludeDiagnosticFee; set { if (_settings.IncludeDiagnosticFee != value) { _settings.IncludeDiagnosticFee = value; OnPropertyChanged(); } } }
        public decimal DiagnosticInspectionFeeSp { get => _settings.DiagnosticInspectionFeeSp; set { if (_settings.DiagnosticInspectionFeeSp != value) { _settings.DiagnosticInspectionFeeSp = value; OnPropertyChanged(); } } }
        public bool WaiveDiagnosticIfRepairProceeds { get => _settings.WaiveDiagnosticIfRepairProceeds; set { if (_settings.WaiveDiagnosticIfRepairProceeds != value) { _settings.WaiveDiagnosticIfRepairProceeds = value; OnPropertyChanged(); } } }

        public bool IncludeLabourCost { get => _settings.IncludeLabourCost; set { if (_settings.IncludeLabourCost != value) { _settings.IncludeLabourCost = value; OnPropertyChanged(); } } }
        public decimal TechnicianHourlyRateSp { get => _settings.TechnicianHourlyRateSp; set { if (_settings.TechnicianHourlyRateSp != value) { _settings.TechnicianHourlyRateSp = value; OnPropertyChanged(); } } }
        public decimal DiagnosisHours { get => _settings.DiagnosisHours; set { if (_settings.DiagnosisHours != value) { _settings.DiagnosisHours = value; OnPropertyChanged(); } } }
        public decimal RepairHours { get => _settings.RepairHours; set { if (_settings.RepairHours != value) { _settings.RepairHours = value; OnPropertyChanged(); } } }
        public decimal ComplexityMultiplier { get => _settings.ComplexityMultiplier; set { if (_settings.ComplexityMultiplier != value) { _settings.ComplexityMultiplier = value; OnPropertyChanged(); } } }

        public bool IncludePartsAndComponents { get => _settings.IncludePartsAndComponents; set { if (_settings.IncludePartsAndComponents != value) { _settings.IncludePartsAndComponents = value; OnPropertyChanged(); } } }
        public decimal DefaultPartsMarkupPercent { get => _settings.DefaultPartsMarkupPercent; set { if (_settings.DefaultPartsMarkupPercent != value) { _settings.DefaultPartsMarkupPercent = value; OnPropertyChanged(); } } }

        public bool IncludeOverheadAllocation { get => _settings.IncludeOverheadAllocation; set { if (_settings.IncludeOverheadAllocation != value) { _settings.IncludeOverheadAllocation = value; OnPropertyChanged(); } } }
        public decimal MonthlyFixedOverheadSp { get => _settings.MonthlyFixedOverheadSp; set { if (_settings.MonthlyFixedOverheadSp != value) { _settings.MonthlyFixedOverheadSp = value; OnPropertyChanged(); } } }
        public decimal AverageJobsPerMonth { get => _settings.AverageJobsPerMonth; set { if (_settings.AverageJobsPerMonth != value) { _settings.AverageJobsPerMonth = value; OnPropertyChanged(); } } }

        public bool IncludeProfitMargin { get => _settings.IncludeProfitMargin; set { if (_settings.IncludeProfitMargin != value) { _settings.IncludeProfitMargin = value; OnPropertyChanged(); } } }
        public decimal DesiredProfitMarginPercent { get => _settings.DesiredProfitMarginPercent; set { if (_settings.DesiredProfitMarginPercent != value) { _settings.DesiredProfitMarginPercent = value; OnPropertyChanged(); } } }
        public bool IncludeVatTax { get => _settings.IncludeVatTax; set { if (_settings.IncludeVatTax != value) { _settings.IncludeVatTax = value; OnPropertyChanged(); } } }
        public decimal VatTaxRatePercent { get => _settings.VatTaxRatePercent; set { if (_settings.VatTaxRatePercent != value) { _settings.VatTaxRatePercent = value; OnPropertyChanged(); } } }

        public ObservableCollection<PricingCostOptionViewModel> CostOptions { get; } = new();
        public string[] ValueTypes { get; } = { PricingService.FixedValueType, PricingService.PercentValueType };

        public ICommand AddCostCommand { get; }
        public ICommand RemoveCostCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }

        public PricingSystemViewModel()
        {
            AddCostCommand = new RelayCommand(_ => AddCost());
            RemoveCostCommand = new RelayCommand(param => RemoveCost(param as PricingCostOptionViewModel));
            SaveCommand = new RelayCommand(_ => Save());
            ResetCommand = new RelayCommand(_ => ResetDefaults());

            Load();
        }

        private void Load()
        {
            var rates = UserPreferencesService.GetExchangeRates();
            UsdRateText = rates.TryGetValue("USD", out var usdRate)
                ? usdRate.ToString(CultureInfo.InvariantCulture)
                : "13000";

            ApplySettings(UserPreferencesService.GetPricingSettings());
        }

        private void ApplySettings(PricingSettings settings)
        {
            _settings = PricingService.NormalizeSettings(settings);
            OnPropertyChanged(string.Empty);
            CostOptions.Clear();
            foreach (var option in _settings.AdditionalCosts)
            {
                CostOptions.Add(PricingCostOptionViewModel.FromModel(option));
            }
        }

        private void AddCost()
        {
            CostOptions.Add(new PricingCostOptionViewModel
            {
                Label = "New cost",
                IsIncluded = false,
                Value = 0m,
                ValueType = PricingService.FixedValueType
            });
        }

        private void RemoveCost(PricingCostOptionViewModel? option)
        {
            if (option == null)
            {
                return;
            }

            CostOptions.Remove(option);
        }

        private void ResetDefaults()
        {
            ApplySettings(PricingService.CreateDefaultSettings());
        }

        private void Save()
        {
            var usdRate = ParseRate(UsdRateText);
            var rates = UserPreferencesService.GetExchangeRates();
            rates["USD"] = usdRate;
            UserPreferencesService.SetExchangeRates(rates);

            _settings.AdditionalCosts = CostOptions.Select(o => o.ToModel()).ToList();
            UserPreferencesService.SetPricingSettings(_settings);
            AppLogger.Success("Pricing system saved.");
        }

        private static decimal ParseRate(string text)
        {
            if (!decimal.TryParse(text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) &&
                !decimal.TryParse(text?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
            {
                throw new System.InvalidOperationException("Please enter a valid USD to S.P exchange rate.");
            }

            if (parsed <= 0)
            {
                throw new System.InvalidOperationException("USD to S.P exchange rate must be greater than 0.");
            }

            return parsed;
        }
    }
}
