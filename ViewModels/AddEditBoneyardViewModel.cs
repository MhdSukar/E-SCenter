using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;

namespace ESCenter.ViewModels
{
    public class AddEditBoneyardViewModel : ObservableObject
    {
        public BoneyardModel Device { get; }

        public ObservableCollection<string> ConditionOptions { get; } = new()
        {
            "New", "Used", "Refurbished", "Dead"
        };

        public ObservableCollection<string> DeviceTypes { get; } = new()
        {
            "Phone", "Tablet", "Laptop", "Other"
        };

        public bool IsEditMode { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddEditBoneyardViewModel()
        {
            // Designer only
        }

        public AddEditBoneyardViewModel(BoneyardModel device, bool isEdit)
        {
            Device = device ?? new BoneyardModel
            {
                DeviceType = -1,
                AddedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            if (Device.DeviceType < 0)
            {
                Device.DeviceType = -1;
            }

            IsEditMode = isEdit;

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Save()
        {
            if (Device.DeviceType < 0)
            {
                System.Windows.MessageBox.Show("Please select a device type.");
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
