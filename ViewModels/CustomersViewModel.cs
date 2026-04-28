using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.ViewModels
{
    public class CustomersViewModel : ObservableObject, IDisposable
    {
        private readonly CustomerRepository _repo;
        private CancellationTokenSource? _searchCts;
        private EventHandler? _databasePathChangedHandler;

        public event Action? FocusFullNameRequested;

        public ObservableCollection<CustomerModel> Customers { get; } = new();

        private CustomerModel? _selectedCustomer;
        public CustomerModel? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    LoadSelectedIntoForm();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    DebounceSearchAsync().FireAndForget(nameof(DebounceSearchAsync));
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isFormDirty;
        public bool IsFormDirty
        {
            get => _isFormDirty;
            set => SetProperty(ref _isFormDirty, value);
        }

        private string _formFullName = string.Empty;
        public string FormFullName
        {
            get => _formFullName;
            set
            {
                if (SetProperty(ref _formFullName, value))
                {
                    IsFormDirty = true;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _formPhoneNumber = string.Empty;
        public string FormPhoneNumber
        {
            get => _formPhoneNumber;
            set
            {
                if (SetProperty(ref _formPhoneNumber, value))
                {
                    IsFormDirty = true;
                }
            }
        }

        private string _formEmail = string.Empty;
        public string FormEmail
        {
            get => _formEmail;
            set
            {
                if (SetProperty(ref _formEmail, value))
                {
                    IsFormDirty = true;
                }
            }
        }

        private string _formAddress = string.Empty;
        public string FormAddress
        {
            get => _formAddress;
            set
            {
                if (SetProperty(ref _formAddress, value))
                {
                    IsFormDirty = true;
                }
            }
        }

        private string _formNotes = string.Empty;
        public string FormNotes
        {
            get => _formNotes;
            set
            {
                if (SetProperty(ref _formNotes, value))
                {
                    IsFormDirty = true;
                }
            }
        }

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public CustomersViewModel()
        {
            var isDesignMode = DesignTimeHelper.IsInDesignMode;
            _repo = AppServices.Get<CustomerRepository>();

            AddCommand = new RelayCommand(_ =>
            {
                ClearForm();
                FocusFullNameRequested?.Invoke();
            });
            SaveCommand = new RelayCommand(_ => SaveAsync().FireAndForget(nameof(SaveAsync)), _ => !string.IsNullOrWhiteSpace(FormFullName));
            DeleteCommand = new RelayCommand(_ => DeleteAsync().FireAndForget(nameof(DeleteAsync)), _ => SelectedCustomer != null);
            ClearCommand = new RelayCommand(_ => ClearForm());
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            if (!isDesignMode)
            {
                _databasePathChangedHandler = (_, _) => LoadAsync().FireAndForget(nameof(LoadAsync));
                DatabasePathService.DatabasePathChanged += _databasePathChangedHandler;
                LoadAsync().FireAndForget(nameof(LoadAsync));
            }
        }

        private async Task DebounceSearchAsync()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token);

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadAsync();
                }
                else
                {
                    var results = await _repo.SearchAsync(SearchText);
                    Customers.Clear();
                    foreach (var customer in results)
                    {
                        Customers.Add(customer);
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Customer search failed: {ex.Message}");
            }
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _repo.GetAllAsync();
                Customers.Clear();
                foreach (var customer in list)
                {
                    Customers.Add(customer);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load customers: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FormFullName))
            {
                AppLogger.Warning("Customer full name is required.");
                return;
            }

            if (SelectedCustomer == null)
            {
                var customer = new CustomerModel
                {
                    FullName = FormFullName,
                    PhoneNumber = FormPhoneNumber,
                    Email = FormEmail,
                    Address = FormAddress,
                    Notes = FormNotes
                };

                await _repo.InsertAsync(customer);
                AppLogger.Success("Customer added.");
            }
            else
            {
                SelectedCustomer.FullName = FormFullName;
                SelectedCustomer.PhoneNumber = FormPhoneNumber;
                SelectedCustomer.Email = FormEmail;
                SelectedCustomer.Address = FormAddress;
                SelectedCustomer.Notes = FormNotes;

                await _repo.UpdateAsync(SelectedCustomer);
                AppLogger.Success("Customer updated.");
            }

            IsFormDirty = false;
            await LoadAsync();
            ClearForm();
        }

        private async Task DeleteAsync()
        {
            if (SelectedCustomer == null)
            {
                return;
            }

            await _repo.DeleteAsync(SelectedCustomer.CustomerId);
            ClearForm();
            AppLogger.Success("Customer deleted.");
            await LoadAsync();
        }

        private void LoadSelectedIntoForm()
        {
            if (SelectedCustomer == null)
            {
                ClearForm();
                return;
            }

            FormFullName = SelectedCustomer.FullName;
            FormPhoneNumber = SelectedCustomer.PhoneNumber;
            FormEmail = SelectedCustomer.Email;
            FormAddress = SelectedCustomer.Address;
            FormNotes = SelectedCustomer.Notes;
            IsFormDirty = false;
        }

        private void ClearForm()
        {
            if (SelectedCustomer != null)
            {
                SelectedCustomer = null;
            }

            FormFullName = string.Empty;
            FormPhoneNumber = string.Empty;
            FormEmail = string.Empty;
            FormAddress = string.Empty;
            FormNotes = string.Empty;
            IsFormDirty = false;
        }

        public void Dispose()
        {
            if (_databasePathChangedHandler != null)
            {
                DatabasePathService.DatabasePathChanged -= _databasePathChangedHandler;
                _databasePathChangedHandler = null;
            }

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;
        }
    }
}
