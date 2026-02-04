using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;

namespace ESCenter.ViewModels
{
    public class AddEditPartViewModel : ObservableObject
    {
        public PartModel Part { get; }

        public ObservableCollection<string> PartTypes { get; } = new()
        {
            "Resistors",
            "Capacitors",
            "ICs",
            "Coils",
            "Ports",
            "Transistors",
            "Diodes"
        };

        public ObservableCollection<string> QualityOptions { get; } = new()
        {
            "Low", "Mid", "High", "Original"
        };

        public ObservableCollection<string> CategoryOptions { get; } = new()
        {
            "Normal",
            "SMD",
            "Through-Hole",
            "Module",
            "Connector"
        };

        public ObservableCollection<string> Unit1Options { get; } = new();
        public ObservableCollection<string> Unit2Options { get; } = new();

        private string _selectedQuality;
        public string SelectedQuality
        {
            get => _selectedQuality;
            set
            {
                if (SetProperty(ref _selectedQuality, value))
                    Part.SetQualityFromDisplay(value);
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsEditMode { get; }

        public AddEditPartViewModel()
        {
            // Designer only
        }

        public AddEditPartViewModel(PartModel part, bool isEdit)
        {
            IsEditMode = isEdit;
            Part = part ?? new PartModel();

            SelectedQuality = Part.QualityDisplay;

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            ApplyUnitDefaults();

            Part.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Part.PartType))
                    ApplyUnitDefaults();
            };
        }

        // -----------------------
        // Units Logic
        // -----------------------
        private void ApplyUnitDefaults()
        {
            Unit1Options.Clear();
            Unit2Options.Clear();

            switch (Part.PartType)
            {
                case "Resistors":
                    Unit1Options.Add("Ω");
                    Unit1Options.Add("kΩ");
                    Unit1Options.Add("MΩ");
                    Unit2Options.Add("V");
                    break;

                case "Capacitors":
                    Unit1Options.Add("pF");
                    Unit1Options.Add("nF");
                    Unit1Options.Add("µF");
                    Unit1Options.Add("mF");
                    Unit2Options.Add("V");
                    break;

                case "Diodes":
                    Unit1Options.Add("V");
                    Unit2Options.Add("A");
                    break;

                case "Transistors":
                    Unit1Options.Add("V");
                    Unit2Options.Add("A");
                    break;

                case "Coils":
                    Unit1Options.Add("µH");
                    Unit1Options.Add("mH");
                    Unit1Options.Add("H");
                    Unit2Options.Add("A");
                    break;

                case "ICs":
                    Unit1Options.Add("V");
                    break;

                default:
                    Unit1Options.Add("");
                    Unit2Options.Add("");
                    break;
            }

            // Auto-assign if empty
            if (string.IsNullOrWhiteSpace(Part.UnitCode1) && Unit1Options.Count > 0)
                Part.UnitCode1 = Unit1Options[0];

            if (string.IsNullOrWhiteSpace(Part.UnitCode2) && Unit2Options.Count > 0)
                Part.UnitCode2 = Unit2Options[0];
        }

        // -----------------------
        // Commands
        // -----------------------
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Part.PartType))
            {
                System.Windows.MessageBox.Show("Please select a part type.");
                return;
            }

            CloseWindow(true);
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        private void CloseWindow(bool result)
        {
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w.DataContext == this)
                {
                    w.DialogResult = result;
                    w.Close();
                    return;
                }
            }
        }
    }
}
