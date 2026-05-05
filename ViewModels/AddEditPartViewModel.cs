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

        private void ApplyUnitDefaults()
        {
            Unit1Options.Clear(); Unit2Options.Clear();
            var db = _typeRepo.GetTypeUnits("Part", Part.PartType ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(db.Unit1)) Unit1Options.Add(db.Unit1);
            if (!string.IsNullOrWhiteSpace(db.Unit2)) Unit2Options.Add(db.Unit2);
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
