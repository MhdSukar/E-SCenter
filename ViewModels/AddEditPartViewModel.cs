using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Windows;

namespace ESCenter.ViewModels
{
    public class AddEditPartViewModel : ObservableObject
    {
        public PartModel Part { get; }
        public ObservableCollection<string> PartTypes { get; } = new();
        public ObservableCollection<string> QualityOptions { get; } = new() { "Low", "Mid", "High", "Original" };
        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> Unit1Options { get; } = new();
        public ObservableCollection<string> Unit2Options { get; } = new();
        public ObservableCollection<string> Currencies { get; } = new() { "S.P", "USD" };

        private string _selectedQuality;
        public string SelectedQuality { get => _selectedQuality; set { if (SetProperty(ref _selectedQuality, value)) Part.SetQualityFromDisplay(value); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ManageCategoriesCommand { get; }
        public bool IsEditMode { get; }

        public AddEditPartViewModel() { }

        public AddEditPartViewModel(PartModel part, bool isEdit)
        {
            IsEditMode = isEdit;
            Part = part ?? new PartModel();
            Part.PriceCurrency = NormalizeCurrency(Part.PriceCurrency);
            SelectedQuality = Part.QualityDisplay;
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
            ManageCategoriesCommand = new RelayCommand(_ => ManageCategories());
            ApplyUnitDefaults();
            Part.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Part.PartType)) ApplyUnitDefaults(); };
            LoadPartTypesAsync().FireAndForget(nameof(LoadPartTypesAsync));
            LoadCategoriesAsync().FireAndForget(nameof(LoadCategoriesAsync));
        }


        private async Task LoadPartTypesAsync()
        {
            var repo = AppServices.Get<CategoryRepository>();
            var partTypes = await repo.GetByTypeAsync("Parts");
            PartTypes.Clear();
            foreach (var type in partTypes)
            {
                PartTypes.Add(type);
            }

            if (!string.IsNullOrWhiteSpace(Part.PartType) && !PartTypes.Contains(Part.PartType))
            {
                Part.PartType = PartTypes.FirstOrDefault() ?? string.Empty;
            }
        }
        private async Task LoadCategoriesAsync()
        {
            var repo = AppServices.Get<CategoryRepository>();
            var categories = await repo.GetByTypeAsync("Parts");
            CategoryOptions.Clear();
            foreach (var c in categories) CategoryOptions.Add(c);
            if (!string.IsNullOrWhiteSpace(Part.Category) && !CategoryOptions.Contains(Part.Category))
                Part.Category = CategoryOptions.FirstOrDefault() ?? "";
        }

        private void ManageCategories()
        {
            var window = new CategoryManagerWindow { Owner = Application.Current.MainWindow };
            window.ShowDialog();
            LoadPartTypesAsync().FireAndForget(nameof(LoadPartTypesAsync));
            LoadCategoriesAsync().FireAndForget(nameof(LoadCategoriesAsync));
        }

        private void ApplyUnitDefaults() { /* unchanged */
            Unit1Options.Clear(); Unit2Options.Clear();
            switch (Part.PartType)
            {
                case "Resistors": Unit1Options.Add("Ω"); Unit1Options.Add("kΩ"); Unit1Options.Add("MΩ"); Unit2Options.Add("V"); break;
                case "Capacitors": Unit1Options.Add("pF"); Unit1Options.Add("nF"); Unit1Options.Add("µF"); Unit1Options.Add("mF"); Unit2Options.Add("V"); break;
                case "Diodes": Unit1Options.Add("V"); Unit2Options.Add("A"); break;
                case "Transistors": Unit1Options.Add("V"); Unit2Options.Add("A"); break;
                case "Coils": Unit1Options.Add("µH"); Unit1Options.Add("mH"); Unit1Options.Add("H"); Unit2Options.Add("A"); break;
                case "ICs": Unit1Options.Add("V"); break;
                default: Unit1Options.Add(""); Unit2Options.Add(""); break;
            }
            if (string.IsNullOrWhiteSpace(Part.UnitCode1) && Unit1Options.Count > 0) Part.UnitCode1 = Unit1Options[0];
            if (string.IsNullOrWhiteSpace(Part.UnitCode2) && Unit2Options.Count > 0) Part.UnitCode2 = Unit2Options[0];
        }

        private void Save() { if (string.IsNullOrWhiteSpace(Part.PartType)) { MessageBox.Show("Please select a part type."); return; } CloseWindow(true); }
        private void Cancel() => CloseWindow(false);
        private void CloseWindow(bool result) { foreach (Window w in Application.Current.Windows) if (w.DataContext == this) { w.DialogResult = result; w.Close(); return; } }
        private static string NormalizeCurrency(string currency) => string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "S.P";
    }
}
