using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class AlBarakaViewModel : ObservableObject
    {
        private readonly AlBarakaDataService _service = AppServices.Get<AlBarakaDataService>();

        public ObservableCollection<AlBarakaRecord> Records { get; } = new();

        private AlBarakaRecord? _selectedRecord;
        public AlBarakaRecord? SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (SetProperty(ref _selectedRecord, value))
                {
                    UpdateFormFromSelection();
                    UpdateCommandStates();
                }
            }
        }

        // Form fields
        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private string _itemName = string.Empty;
        public string ItemName
        {
            get => _itemName;
            set
            {
                if (SetProperty(ref _itemName, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        private decimal? _price;
        public decimal? Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    RefreshPriceEquivalent();
                    UpdateCommandStates();
                }
            }
        }

        private string _priceCurrency = "S.P";
        public string PriceCurrency
        {
            get => _priceCurrency;
            set
            {
                if (SetProperty(ref _priceCurrency, value))
                {
                    RefreshPriceEquivalent();
                }
            }
        }

        private string _category = "Consumable";
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _account = string.Empty;
        public string Account
        {
            get => _account;
            set => SetProperty(ref _account, value);
        }

        private string _priceEquivalent = string.Empty;
        public string PriceEquivalent
        {
            get => _priceEquivalent;
            set => SetProperty(ref _priceEquivalent, value);
        }

        // Filters
        private DateTime? _filterStart;
        public DateTime? FilterStart
        {
            get => _filterStart;
            set { if (SetProperty(ref _filterStart, value)) Refresh(); }
        }

        private DateTime? _filterEnd;
        public DateTime? FilterEnd
        {
            get => _filterEnd;
            set { if (SetProperty(ref _filterEnd, value)) Refresh(); }
        }

        // Totals for filtered records
        private decimal _totalSP;
        public decimal TotalSP
        {
            get => _totalSP;
            set => SetProperty(ref _totalSP, value);
        }

        private decimal _totalUSD;
        public decimal TotalUSD
        {
            get => _totalUSD;
            set => SetProperty(ref _totalUSD, value);
        }

        private string _filterCategory = "All";
        public string FilterCategory
        {
            get => _filterCategory;
            set { if (SetProperty(ref _filterCategory, value)) Refresh(); }
        }

        public string[] Categories { get; } = new[] { "All", "Consumable", "Tools", "Devices", "Doner Purchase" };
        public string[] Currencies { get; } = new[] { "S.P", "USD" };
        public string[] Accounts { get; } = new[] { "Cash", "Bank", "Other" };

        // Commands
        public ICommand AddNewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenExchangeRatesCommand { get; }

        public AlBarakaViewModel()
        {
            AddNewCommand = new RelayCommand(_ => ClearForm(preserveDate: true));
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedRecord != null);
            RefreshCommand = new RelayCommand(_ => Refresh());
            OpenExchangeRatesCommand = new RelayCommand(_ => OpenExchangeRates());

            // default filters: beginning and end of current month
            var today = DateTime.Today;
            FilterStart = new DateTime(today.Year, today.Month, 1);
            FilterEnd = FilterStart.Value.AddMonths(1).AddDays(-1);
            Date = today;

            Refresh();
        }

        private void UpdateFormFromSelection()
        {
            if (SelectedRecord != null)
            {
                Date = SelectedRecord.Date;
                ItemName = SelectedRecord.ItemName;
                Price = SelectedRecord.Price;
                PriceCurrency = SelectedRecord.PriceCurrency;
                Category = SelectedRecord.Category;
                Account = SelectedRecord.Account;
            }
            else
            {
                ClearForm(preserveDate: true);
            }

            NotifyAllProperties();
        }

        private void NotifyAllProperties()
        {
            OnPropertyChanged(nameof(Date));
            OnPropertyChanged(nameof(ItemName));
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(PriceCurrency));
            OnPropertyChanged(nameof(PriceEquivalent));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(Account));
        }

        private void ClearForm(bool preserveDate = false)
        {
            var selectedDate = Date;

            SelectedRecord = null;
            Date = preserveDate ? selectedDate : DateTime.Today;
            ItemName = string.Empty;
            Price = null;
            PriceCurrency = "S.P";
            Category = "Consumable";
            Account = string.Empty;
            RefreshPriceEquivalent();
            NotifyAllProperties();
            UpdateCommandStates();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ItemName) && Price.HasValue;
        }

        private void Save()
        {
            try
            {
                if (SelectedRecord == null)
                {
                    var rec = new AlBarakaRecord
                    {
                        Date = Date,
                        ItemName = ItemName,
                        Price = Price,
                        PriceCurrency = PriceCurrency,
                        Category = Category,
                        Account = Account?.Trim() ?? string.Empty
                    };

                    var id = _service.Insert(rec);
                    rec.AlBarakaId = id;
                    Refresh();
                    ClearForm(preserveDate: true);
                }
                else
                {
                    SelectedRecord.Date = Date;
                    SelectedRecord.ItemName = ItemName;
                    SelectedRecord.Price = Price;
                    SelectedRecord.PriceCurrency = PriceCurrency;
                    SelectedRecord.Category = Category;
                    SelectedRecord.Account = Account?.Trim() ?? string.Empty;

                    _service.Update(SelectedRecord);
                    Refresh();
                }

                AppLogger.Success("Al-Baraka record saved.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to save Al-Baraka record: {ex.Message}");
            }
            UpdateCommandStates();
        }

        private void Delete()
        {
            if (SelectedRecord == null) return;

            try
            {
                _service.Delete(SelectedRecord.AlBarakaId);
                Records.Remove(SelectedRecord);
                ClearForm();
                AppLogger.Success("Al-Baraka record deleted.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to delete Al-Baraka record: {ex.Message}");
            }

            UpdateCommandStates();
        }

        public void Refresh()
        {
            try
            {
                Records.Clear();
                var all = _service.GetAll();

                var filtered = all.AsQueryable();
                if (FilterStart.HasValue)
                {
                    filtered = filtered.Where(r => r.Date >= FilterStart.Value.Date);
                }
                if (FilterEnd.HasValue)
                {
                    filtered = filtered.Where(r => r.Date <= FilterEnd.Value.Date.AddDays(1).AddTicks(-1));
                }
                if (!string.IsNullOrWhiteSpace(FilterCategory) && FilterCategory != "All")
                {
                    filtered = filtered.Where(r => string.Equals(r.Category, FilterCategory, StringComparison.OrdinalIgnoreCase));
                }

                var list = filtered.ToList();
                foreach (var r in list)
                {
                    Records.Add(r);
                }

                // Compute totals based on the currently filtered items
                TotalSP = list.Where(r => string.Equals(r.PriceCurrency, "S.P", StringComparison.OrdinalIgnoreCase) && r.Price.HasValue)
                              .Sum(r => r.Price ?? 0);
                TotalUSD = list.Where(r => string.Equals(r.PriceCurrency, "USD", StringComparison.OrdinalIgnoreCase) && r.Price.HasValue)
                               .Sum(r => r.Price ?? 0);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to refresh Al-Baraka records: {ex.Message}");
            }
        }

        private void UpdateCommandStates()
        {
            // Raise can execute changed by recreating commands or using a RelayCommand with canExecute notifications.
            // Simplest: notify properties that commands depend on
            OnPropertyChanged(nameof(SelectedRecord));
        }

        private void RefreshPriceEquivalent()
        {
            var rates = UserPreferencesService.GetExchangeRates();
            PriceEquivalent = ComputeEquivalent(Price, PriceCurrency, rates);
        }

        private static string ComputeEquivalent(decimal? amount, string currency, System.Collections.Generic.Dictionary<string, decimal> rates)
        {
            if (amount == null || amount == 0)
            {
                return string.Empty;
            }

            if (currency == "S.P")
            {
                return string.Empty;
            }

            if (!rates.TryGetValue(currency ?? string.Empty, out var rate) || rate == 0)
            {
                return string.Empty;
            }

            var spEquivalent = amount.Value * rate;
            return $"≈ {spEquivalent:N0} S.P";
        }

        private void OpenExchangeRates()
        {
            var window = new Windows.ExchangeRatesWindow
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                RefreshPriceEquivalent();
                AppLogger.Success("Exchange rates updated.");
            }
        }
    }
}
