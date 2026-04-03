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
        private readonly AlBarakaDataService _service = new AlBarakaDataService();

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
        public DateTime Date { get; set; } = DateTime.Today;
        public string ItemName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string PriceCurrency { get; set; } = "S.P";
        public string Category { get; set; } = "Consumable";
        public string Account { get; set; } = string.Empty;

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

        public AlBarakaViewModel()
        {
            AddNewCommand = new RelayCommand(_ => ClearForm(preserveDate: true));
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedRecord != null);
            RefreshCommand = new RelayCommand(_ => Refresh());

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
                        Account = Account
                    };

                    var id = _service.Insert(rec);
                    rec.AlBarakaId = id;
                    Records.Insert(0, rec);
                    ClearForm(preserveDate: true);
                }
                else
                {
                    SelectedRecord.Date = Date;
                    SelectedRecord.ItemName = ItemName;
                    SelectedRecord.Price = Price;
                    SelectedRecord.PriceCurrency = PriceCurrency;
                    SelectedRecord.Category = Category;
                    SelectedRecord.Account = Account;

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
    }
}
