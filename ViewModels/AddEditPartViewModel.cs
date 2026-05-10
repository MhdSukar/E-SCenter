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
    public class AddEditPartViewModel : ObservableObject
    {
        private readonly TypeDefinitionsRepository _typeRepo = new();
        public PartModel Part { get; }

        public ObservableCollection<string> PartTypes { get; } = new();
        public ObservableCollection<string> QualityOptions { get; } = new() { "Low", "Mid", "High", "Original" };
        public ObservableCollection<string> CategoryOptions { get; } = new() { "Normal", "SMD", "Through-Hole", "Module", "Connector" };
        public ObservableCollection<string> Unit1Options { get; } = new();
        public ObservableCollection<string> Unit2Options { get; } = new();
        public ObservableCollection<string> Currencies { get; } = new() { "S.P", "USD" };
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddNewTypeCommand { get; }
        public bool IsEditMode { get; }
        public bool CanAddNewType => string.Equals(Part?.PartType, "Other", StringComparison.OrdinalIgnoreCase);

        private string _selectedQuality;
        public string SelectedQuality { get => _selectedQuality; set { if (SetProperty(ref _selectedQuality, value)) Part.SetQualityFromDisplay(value); } }

        public AddEditPartViewModel() { }
        public AddEditPartViewModel(PartModel part, bool isEdit)
        {
            IsEditMode = isEdit;
            Part = part ?? new PartModel();
            Part.PriceCurrency = NormalizeCurrency(Part.PriceCurrency);
            SelectedQuality = Part.QualityDisplay;
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddNewTypeCommand = new RelayCommand(_ => AddNewType(), _ => CanAddNewType);
            LoadTypes();
            ApplyUnitDefaults();
            Part.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Part.PartType)) { OnPropertyChanged(nameof(CanAddNewType)); ApplyUnitDefaults(); } };
        }

        private void LoadTypes()
        {
            var defaults = new[] { "Resistors", "Capacitors", "ICs", "Coils", "Ports", "Transistors", "Diodes" };
            var types = _typeRepo.GetTypes("Part");
            foreach (var t in defaults.Concat(types).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x)) PartTypes.Add(t);
            PartTypes.Add("Other");
        }

        private void AddNewType()
        {
            var win = new Views.AddTypeDefinitionWindow(true) { Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) };
            if (win.ShowDialog() == true)
            {
                _typeRepo.UpsertType("Part", win.TypeNameValue, win.Unit1Value, win.Unit2Value);
                if (!PartTypes.Contains(win.TypeNameValue)) PartTypes.Insert(Math.Max(0, PartTypes.Count - 1), win.TypeNameValue);
                Part.PartType = win.TypeNameValue;
                ApplyUnitDefaults();
            }
        }

        // Hardcoded unit defaults for built-in part types
        private static readonly Dictionary<string, (string Unit1, string Unit2)> _builtInTypeUnits =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Resistors"] = ("Ω", "W"),
                ["Capacitors"] = ("μF", "V"),
                ["Coils"] = ("μH", "A"),
                ["Transistors"] = ("V", "A"),
                ["Diodes"] = ("V", "A"),
                ["ICs"] = ("", ""),
                ["Ports"] = ("", ""),
            };

        private void ApplyUnitDefaults()
        {
            Unit1Options.Clear(); Unit2Options.Clear();

            string unit1, unit2;

            if (_builtInTypeUnits.TryGetValue(Part.PartType ?? string.Empty, out var builtIn))
            {
                // Known hardcoded type — use predefined units
                unit1 = builtIn.Unit1;
                unit2 = builtIn.Unit2;
            }
            else
            {
                // Custom type added via wizard — check the database
                var db = _typeRepo.GetTypeUnits("Part", Part.PartType ?? string.Empty);
                unit1 = db.Unit1;
                unit2 = db.Unit2;
            }

            if (!string.IsNullOrWhiteSpace(unit1)) Unit1Options.Add(unit1);
            if (!string.IsNullOrWhiteSpace(unit2)) Unit2Options.Add(unit2);

            // Always include the part's existing codes so editing an old part doesn't lose them
            if (!string.IsNullOrWhiteSpace(Part.UnitCode1) && !Unit1Options.Contains(Part.UnitCode1))
                Unit1Options.Add(Part.UnitCode1);
            if (!string.IsNullOrWhiteSpace(Part.UnitCode2) && !Unit2Options.Contains(Part.UnitCode2))
                Unit2Options.Add(Part.UnitCode2);

            if (Unit1Options.Count == 0) Unit1Options.Add("");
            if (Unit2Options.Count == 0) Unit2Options.Add("");

            if (string.IsNullOrWhiteSpace(Part.UnitCode1)) Part.UnitCode1 = Unit1Options[0];
            if (string.IsNullOrWhiteSpace(Part.UnitCode2)) Part.UnitCode2 = Unit2Options[0];
        }

        private void Save() { if (string.IsNullOrWhiteSpace(Part.PartType) || Part.PartType == "Other") { System.Windows.MessageBox.Show("Please select a valid part type."); return; } CloseWindow(true); }
        private void Cancel() => CloseWindow(false);
        private void CloseWindow(bool result) { foreach (Window w in System.Windows.Application.Current.Windows) if (w.DataContext == this) { w.DialogResult = result; w.Close(); return; } }
        private static string NormalizeCurrency(string currency) => string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "S.P";


    }
}
