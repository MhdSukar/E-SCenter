using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Views;

namespace ESCenter.ViewModels
{
    public class BoneyardViewModel : ObservableObject
    {
        private readonly BoneyardRepository _repo = new();

        // =====================
        // Collections & Selection
        // =====================
        public ObservableCollection<BoneyardModel> Devices { get; } = new();

        private BoneyardModel _selectedDevice;
        public BoneyardModel SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                SetProperty(ref _selectedDevice, value);
                CommandManager.InvalidateRequerySuggested(); // update command states
            }
        }

        // =====================
        // Commands
        // =====================
        public ICommand RefreshCommand { get; }
        public ICommand AddDeviceCommand { get; }
        public ICommand EditDeviceCommand { get; }
        public ICommand DeleteDeviceCommand { get; }

        // =====================
        // Search
        // =====================
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplySearchFilter();
            }
        }

        // =====================
        // Constructor
        // =====================
        public BoneyardViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadDevices());
            AddDeviceCommand = new RelayCommand(_ => AddDevice());
            EditDeviceCommand = new RelayCommand(_ => EditDevice(), _ => SelectedDevice != null);
            DeleteDeviceCommand = new RelayCommand(_ => DeleteDevice(), _ => SelectedDevice != null);

            LoadDevices();
        }

        // =====================
        // Load & Refresh
        // =====================
        private void LoadDevices()
        {
            Devices.Clear();
            foreach (var d in _repo.GetAll())
                Devices.Add(d);
        }

        // =====================
        // Add / Edit Device
        // =====================
        private void AddDevice()
        {
            var device = new BoneyardModel
            {
                DeviceType = 0,
                AddedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            var vm = new AddEditBoneyardViewModel(device, false);
            var win = new AddEditBoneyardWindow { DataContext = vm };

            if (win.ShowDialog() == true)
            {
                device.DeviceId = _repo.Insert(device);
                Devices.Insert(0, device);
                SelectedDevice = device;
            }
        }

        private void EditDevice()
        {
            if (SelectedDevice == null) return;

            // Clone to allow canceling edits
            var clone = new BoneyardModel
            {
                DeviceId = SelectedDevice.DeviceId,
                DeviceType = SelectedDevice.DeviceType,
                Brand = SelectedDevice.Brand,
                Model = SelectedDevice.Model,
                Condition = SelectedDevice.Condition,
                HolderID = SelectedDevice.HolderID,
                Notes = SelectedDevice.Notes,
                AddedAt = SelectedDevice.AddedAt
            };

            var vm = new AddEditBoneyardViewModel(clone, true);
            var win = new AddEditBoneyardWindow { DataContext = vm };

            if (win.ShowDialog() == true)
            {
                // Copy changes back
                SelectedDevice.DeviceType = clone.DeviceType;
                SelectedDevice.Brand = clone.Brand;
                SelectedDevice.Model = clone.Model;
                SelectedDevice.Condition = clone.Condition;
                SelectedDevice.HolderID = clone.HolderID;
                SelectedDevice.Notes = clone.Notes;
                SelectedDevice.AddedAt = clone.AddedAt;

                _repo.Update(SelectedDevice);
                LoadDevices();
            }
        }

        // =====================
        // Delete Device
        // =====================
        private void DeleteDevice()
        {
            if (SelectedDevice == null) return;

            _repo.Delete(SelectedDevice.DeviceId);
            Devices.Remove(SelectedDevice);
            SelectedDevice = null;
        }

        // =====================
        // Search / Filter
        // =====================
        private void ApplySearchFilter()
        {
            Devices.Clear();
            var allDevices = _repo.GetAll();

            var filtered = string.IsNullOrWhiteSpace(_searchText)
                ? allDevices
                : allDevices.Where(d =>
                    (d.Brand?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Model?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Condition?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.HolderID?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Notes?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                );

            foreach (var d in filtered)
                Devices.Add(d);
        }
    }
}
