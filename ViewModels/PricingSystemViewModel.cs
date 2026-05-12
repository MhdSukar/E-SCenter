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

            CostOptions.Clear();
            foreach (var option in UserPreferencesService.GetPricingCostOptions())
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
            CostOptions.Clear();
            foreach (var option in PricingService.CreateDefaultOptions())
            {
                CostOptions.Add(PricingCostOptionViewModel.FromModel(option));
            }
        }

        private void Save()
        {
            var usdRate = ParseRate(UsdRateText);
            var rates = UserPreferencesService.GetExchangeRates();
            rates["USD"] = usdRate;
            UserPreferencesService.SetExchangeRates(rates);
            UserPreferencesService.SetPricingCostOptions(CostOptions.Select(o => o.ToModel()));
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
