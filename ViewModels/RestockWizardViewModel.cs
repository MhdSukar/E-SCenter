using System;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;

namespace ESCenter.ViewModels
{
    public class RestockWizardViewModel : ObservableObject
    {
        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            private set => SetProperty(ref _itemName, value);
        }

        private string _source;
        public string Source
        {
            get => _source;
            private set => SetProperty(ref _source, value);
        }

        private int _currentQty;
        public int CurrentQty
        {
            get => _currentQty;
            private set => SetProperty(ref _currentQty, value);
        }

        private int _restockQuantity = 10;
        public int RestockQuantity
        {
            get => _restockQuantity;
            set
            {
                if (SetProperty(ref _restockQuantity, value))
                {
                    OnPropertyChanged(nameof(TotalAfterRestock));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _restockNote = string.Empty;
        public string RestockNote
        {
            get => _restockNote;
            set => SetProperty(ref _restockNote, value);
        }

        public int TotalAfterRestock => CurrentQty + RestockQuantity;

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public RestockWizardViewModel(string itemName, string source, int currentQty)
        {
            ItemName = itemName;
            Source = source;
            CurrentQty = currentQty;

            ConfirmCommand = new RelayCommand(_ => RestockAsync().FireAndForget(nameof(RestockAsync)), _ => RestockQuantity > 0);
            CancelCommand = new RelayCommand(_ => CloseWindow(false));
        }

        private async System.Threading.Tasks.Task RestockAsync()
        {
            try
            {
                if (Source == "Parts")
                {
                    var repo = AppServices.Get<PartsRepository>();
                    await repo.RestoreQuantityByNameAsync(ItemName, RestockQuantity);
                }
                else
                {
                    var repo = AppServices.Get<InventoryRepository>();
                    await repo.RestoreQuantityByNameAsync(ItemName, RestockQuantity);
                }

                AppLogger.Success($"Restocked '{ItemName}' by {RestockQuantity} unit(s).");
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Restock failed: {ex.Message}");
            }
        }

        private void CloseWindow(bool result)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = result;
                    window.Close();
                    return;
                }
            }
        }
    }
}
