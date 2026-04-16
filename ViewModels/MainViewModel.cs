using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Windows.Input;
using System.Windows.Threading;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;
using ESCenter.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ESCenter.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private ICommand? _requestAdminAccessCommand;
        public ICommand RequestAdminAccessCommand => _requestAdminAccessCommand ??= new RelayCommand(_ => RequestAdminAccess());

        private ICommand? _initializeDatabaseCommand;
        public ICommand InitializeDatabaseCommand => _initializeDatabaseCommand ??= new RelayCommand(_ => InitializeDatabase());

        private ICommand? _loadDatabaseCommand;
        public ICommand LoadDatabaseCommand => _loadDatabaseCommand ??= new RelayCommand(_ => LoadDatabase());

        private ICommand? _changeAdminPasswordCommand;
        public ICommand ChangeAdminPasswordCommand => _changeAdminPasswordCommand ??= new RelayCommand(_ => ChangeAdminPassword());

        private ICommand? _resetSettingsCommand;
        public ICommand ResetSettingsCommand => _resetSettingsCommand ??= new RelayCommand(_ => ResetSettings());

        private ICommand? _resetDatabaseCommand;
        public ICommand ResetDatabaseCommand => _resetDatabaseCommand ??= new RelayCommand(_ => ResetDatabase());

        private ICommand? _backupNowCommand;
        public ICommand BackupNowCommand => _backupNowCommand ??= new RelayCommand(_ => BackupNow());

        private ICommand? _refreshAllCommand;
        public ICommand RefreshAllCommand => _refreshAllCommand ??= new RelayCommand(_ => RefreshAll());

        private bool _isAdminAccessGranted;
        public bool IsAdminAccessGranted
        {
            get => _isAdminAccessGranted;
            set => SetProperty(ref _isAdminAccessGranted, value);
        }

        private void ShowSettings()
        {
            var window = new Windows.SettingsWindow
            {
                Owner = System.Windows.Application.Current.MainWindow,
                DataContext = new SettingsViewModel()
            };

            window.ShowDialog();
        }

        private bool _autoGrantAdminAccess;
        public bool AutoGrantAdminAccess
        {
            get => _autoGrantAdminAccess;
            set
            {
                if (SetProperty(ref _autoGrantAdminAccess, value))
                {
                    // Persist preference
                    ESCenter.Services.UserPreferencesService.SetAutoGrantAdminAccess(value);
                    // If enabled, grant admin access immediately; if disabled, revoke it so login is required again
                    IsAdminAccessGranted = value;
                }
            }
        }

        private bool _launchAtWindowsStartup;
        public bool LaunchAtWindowsStartup
        {
            get => _launchAtWindowsStartup;
            set
            {
                if (!SetProperty(ref _launchAtWindowsStartup, value))
                {
                    return;
                }

                UserPreferencesService.SetLaunchAtWindowsStartup(value);

                if (!WindowsStartupService.SetLaunchAtStartup(value))
                {
                    _launchAtWindowsStartup = WindowsStartupService.IsLaunchAtStartupEnabled();
                    OnPropertyChanged(nameof(LaunchAtWindowsStartup));
                    AppLogger.Warning("Unable to update Windows startup setting.");
                    return;
                }

                AppLogger.Success(value
                    ? "Launch at Windows startup enabled."
                    : "Launch at Windows startup disabled.");
            }
        }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowRepairTicketsCommand { get; }
        public ICommand ShowOverdueTicketsCommand { get; }
        public ICommand ShowCriticalTicketsCommand { get; }
        public ICommand ShowReadyPickupTicketsCommand { get; }
        public ICommand NewTicketCommand { get; }
        public ICommand ShowPartsControlCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowAlBarakaCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand ShowBoneyardCommand { get; }
        public ICommand ShowWarrantySystemCommand { get; }
        public ICommand ShowUserProfileCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand OpenLogFolderCommand { get; }
        public ICommand NavigateToResultCommand { get; }
        public ICommand CloseGlobalSearchCommand { get; }
        public ICommand GlobalSearchMoveDownCommand { get; }
        public ICommand GlobalSearchMoveUpCommand { get; }
        public ICommand GlobalSearchAcceptCommand { get; }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private NavSection _activeSection = NavSection.Dashboard;
        public NavSection ActiveSection
        {
            get => _activeSection;
            set
            {
                if (!SetProperty(ref _activeSection, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(IsDashboardSelected));
                OnPropertyChanged(nameof(IsRepairTicketsSelected));
                OnPropertyChanged(nameof(IsPartsControlSelected));
                OnPropertyChanged(nameof(IsInventorySelected));
                OnPropertyChanged(nameof(IsBoneyardSelected));
            }
        }

        public bool IsDashboardSelected => ActiveSection == NavSection.Dashboard;
        public bool IsRepairTicketsSelected => ActiveSection == NavSection.RepairTickets;
        public bool IsPartsControlSelected => ActiveSection == NavSection.PartsControl;
        public bool IsInventorySelected => ActiveSection == NavSection.Inventory;
        public bool IsBoneyardSelected => ActiveSection == NavSection.Boneyard;

        private string _statusText = "System Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private StatusLevel _currentStatusLevel = StatusLevel.Info;
        public StatusLevel CurrentStatusLevel
        {
            get => _currentStatusLevel;
            set => SetProperty(ref _currentStatusLevel, value);
        }

        private string _databaseConnectionStatusText = "Offline";
        public string DatabaseConnectionStatusText
        {
            get => _databaseConnectionStatusText;
            set => SetProperty(ref _databaseConnectionStatusText, value);
        }

        private System.Windows.Media.Brush _databaseConnectionBrush = System.Windows.Media.Brushes.IndianRed;
        public System.Windows.Media.Brush DatabaseConnectionBrush
        {
            get => _databaseConnectionBrush;
            set => SetProperty(ref _databaseConnectionBrush, value);
        }

        private string _databaseFileSizeText = string.Empty;
        public string DatabaseFileSizeText
        {
            get => _databaseFileSizeText;
            set => SetProperty(ref _databaseFileSizeText, value);
        }

        private int _expiringWarrantyCount;
        public int ExpiringWarrantyCount
        {
            get => _expiringWarrantyCount;
            set => SetProperty(ref _expiringWarrantyCount, value);
        }

        private string _lastBackupText = "Unknown";
        public string LastBackupText
        {
            get => _lastBackupText;
            set => SetProperty(ref _lastBackupText, value);
        }

        private string _clockText = DateTime.Now.ToString("HH:mm:ss");
        public string ClockText
        {
            get => _clockText;
            set => SetProperty(ref _clockText, value);
        }

        public string CurrentUser => "мн∂ ѕυкαя";
        public System.Windows.Media.Brush UsernameBrush { get; } = System.Windows.Media.Brushes.DeepSkyBlue;

        public DashboardViewModel Dashboard { get; }

        public ObservableCollection<CurveSeries> TicketStatusCurves { get; } = new();

        private ISeries[] _ticketsSeries = Array.Empty<ISeries>();
        public ISeries[] TicketsSeries
        {
            get => _ticketsSeries;
            set => SetProperty(ref _ticketsSeries, value);
        }

        private Axis[] _ticketsXAxis = Array.Empty<Axis>();
        public Axis[] TicketsXAxis
        {
            get => _ticketsXAxis;
            set => SetProperty(ref _ticketsXAxis, value);
        }

        private Axis[] _ticketsYAxis = Array.Empty<Axis>();
        public Axis[] TicketsYAxis
        {
            get => _ticketsYAxis;
            set => SetProperty(ref _ticketsYAxis, value);
        }

        public SolidColorPaint TicketsTooltipTextPaint { get; } = new(SKColors.White);
        public SolidColorPaint TicketsTooltipBackgroundPaint { get; } = new(new SKColor(11, 26, 42, 204));

        public ICommand ToggleCurveVisibilityCommand { get; }

        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _statusResetTimer;
        private readonly DispatcherTimer _databaseStatusTimer;
        private readonly TicketsDataService _chartDataService;
        private RepairTicketsViewModel? _repairTicketsViewModel;
        private PartsControlViewModel? _partsControlViewModel;
        private InventoryViewModel? _inventoryViewModel;
        private BoneyardViewModel? _boneyardViewModel;
        private ReportsViewModel? _reportsViewModel;
        private AlBarakaViewModel? _alBarakaViewModel;
        private UserProfileWindow? _userProfileWindow;
        private readonly GlobalSearchService _globalSearchService;

        private string _globalSearchQuery = string.Empty;
        public string GlobalSearchQuery
        {
            get => _globalSearchQuery;
            set
            {
                if (SetProperty(ref _globalSearchQuery, value))
                {
                    RunGlobalSearch();
                }
            }
        }

        private bool _isGlobalSearchOpen;
        public bool IsGlobalSearchOpen
        {
            get => _isGlobalSearchOpen;
            set => SetProperty(ref _isGlobalSearchOpen, value);
        }

        private int _globalSearchSelectedIndex = -1;
        public int GlobalSearchSelectedIndex
        {
            get => _globalSearchSelectedIndex;
            set => SetProperty(ref _globalSearchSelectedIndex, value);
        }

        public ObservableCollection<GlobalSearchResult> GlobalSearchResults { get; } = new();

        public MainViewModel()
        {
            var isDesignMode = DesignTimeHelper.IsInDesignMode;
            Dashboard = new DashboardViewModel();
            _chartDataService = AppServices.IsInitialized
                ? AppServices.Get<TicketsDataService>()
                : new TicketsDataService();
            _globalSearchService = AppServices.IsInitialized
                ? AppServices.Get<GlobalSearchService>()
                : new GlobalSearchService();
            // Load persisted preference for auto-granting admin access on startup
            _autoGrantAdminAccess = ESCenter.Services.UserPreferencesService.GetAutoGrantAdminAccess();
            if (_autoGrantAdminAccess)
            {
                IsAdminAccessGranted = true;
            }

            _launchAtWindowsStartup = UserPreferencesService.GetLaunchAtWindowsStartup();
            var startupApplyResult = isDesignMode || WindowsStartupService.SetLaunchAtStartup(_launchAtWindowsStartup);
            if (!startupApplyResult && !isDesignMode)
            {
                _launchAtWindowsStartup = WindowsStartupService.IsLaunchAtStartupEnabled();
                UserPreferencesService.SetLaunchAtWindowsStartup(_launchAtWindowsStartup);
            }

            ShowDashboardCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.Dashboard;
                Navigate(Dashboard, "Dashboard loaded");
            });

            ShowRepairTicketsCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.RepairTickets;
                Navigate(GetOrCreateRepairTicketsViewModel(), "Tickets loaded");
            });

            ShowOverdueTicketsCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.RepairTickets;
                var vm = GetOrCreateRepairTicketsViewModel();
                vm.ShowOverdueFromIntegration();
                Navigate(vm, "Showing overdue tickets");
            });

            ShowCriticalTicketsCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.RepairTickets;
                var vm = GetOrCreateRepairTicketsViewModel();
                vm.ShowCriticalFromIntegration();
                Navigate(vm, "Showing critical tickets");
            });

            ShowReadyPickupTicketsCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.RepairTickets;
                var vm = GetOrCreateRepairTicketsViewModel();
                vm.ShowReadyPickupsFromIntegration();
                Navigate(vm, "Showing ready for pickup tickets");
            });

            NewTicketCommand = new RelayCommand(_ =>
            {
                ShowRepairTicketsCommand.Execute(null);
                _repairTicketsViewModel?.ClearForm();
            });

            ShowPartsControlCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.PartsControl;
                Navigate(GetOrCreatePartsControlViewModel(), "Parts Control loaded");
            });

            ShowReportsCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.None;
                Navigate(GetOrCreateReportsViewModel(), "Reports loaded");
            });

            ShowAlBarakaCommand = new RelayCommand(_ =>
            {
                // Open internal Al-Baraka view (replaces external launcher)
                ActiveSection = NavSection.None;
                Navigate(GetOrCreateAlBarakaViewModel(), "Al-Baraka loaded");
            });

            ShowInventoryCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.Inventory;
                Navigate(GetOrCreateInventoryViewModel(), "Inventory loaded");
            });

            ShowBoneyardCommand = new RelayCommand(_ =>
            {
                ActiveSection = NavSection.Boneyard;
                Navigate(GetOrCreateBoneyardViewModel(), "Boneyard loaded");
            });

            ShowWarrantySystemCommand = new RelayCommand(_ =>
            {
                // warranty is a window - deselect all tabs
                ActiveSection = NavSection.None;
                ShowWarrantySystem();
            });

            ShowUserProfileCommand = new RelayCommand(_ => ShowUserProfile());
            ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
            OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
            NavigateToResultCommand = new RelayCommand(param =>
            {
                if (param is GlobalSearchResult result)
                {
                    NavigateToResult(result);
                }
            });

            CloseGlobalSearchCommand = new RelayCommand(_ =>
            {
                IsGlobalSearchOpen = false;
                GlobalSearchSelectedIndex = -1;
            });

            GlobalSearchMoveDownCommand = new RelayCommand(_ =>
            {
                if (GlobalSearchResults.Count == 0)
                {
                    return;
                }

                GlobalSearchSelectedIndex = Math.Min(GlobalSearchSelectedIndex + 1, GlobalSearchResults.Count - 1);
            });

            GlobalSearchMoveUpCommand = new RelayCommand(_ =>
            {
                GlobalSearchSelectedIndex = Math.Max(GlobalSearchSelectedIndex - 1, -1);
            });

            GlobalSearchAcceptCommand = new RelayCommand(_ =>
            {
                if (GlobalSearchSelectedIndex >= 0 && GlobalSearchSelectedIndex < GlobalSearchResults.Count)
                {
                    NavigateToResult(GlobalSearchResults[GlobalSearchSelectedIndex]);
                }
                else if (!string.IsNullOrWhiteSpace(GlobalSearchQuery) && GlobalSearchResults.Count > 0)
                {
                    NavigateToResult(GlobalSearchResults[0]);
                }
            });

            // F-key bindings use these commands (declared separately so XAML can bind by name)
            if (!isDesignMode)
            {
                AppEvents.DashboardRefreshRequested += () => Dashboard.Refresh();
                AppEvents.NavigateToTicketsRequested += () => ShowRepairTicketsCommand.Execute(null);
            }

            CurrentView = Dashboard;
            SetStatus("System Ready", StatusLevel.Info);

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => ClockText = DateTime.Now.ToString("HH:mm:ss");
            if (!isDesignMode)
            {
                _clockTimer.Start();
            }

            _statusResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _statusResetTimer.Tick += (_, _) =>
            {
                _statusResetTimer.Stop();
                SetStatus("System Ready", StatusLevel.Info);
            };

            _databaseStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _databaseStatusTimer.Tick += async (_, _) =>
            {
                await RefreshDatabaseConnectionStatusAsync();
                RefreshDatabaseFileSizeAndBackupInfo();
            };
            if (!isDesignMode)
            {
                _databaseStatusTimer.Start();
                RefreshDatabaseFileSizeAndBackupInfo();
            }

            if (!isDesignMode)
            {
                AppLogger.StatusRaised += OnStatusRaised;
                TicketEvents.TicketsChanged += async (_, _) =>
                {
                    await RefreshTicketsChartAsync();
                    await RefreshWarrantyAlertCountAsync();
                };
                DatabasePathService.DatabasePathChanged += async (_, _) =>
                {
                    await RefreshTicketsChartAsync();
                    await RefreshWarrantyAlertCountAsync();
                    await RefreshDatabaseConnectionStatusAsync();
                    RefreshDatabaseFileSizeAndBackupInfo();
                };
            }

            ToggleCurveVisibilityCommand = new RelayCommand(param =>
            {
                var key = param as string;
                if (string.IsNullOrWhiteSpace(key))
                {
                    return;
                }

                var curve = TicketStatusCurves.FirstOrDefault(c => c.Key == key);
                if (curve is not null)
                {
                    curve.IsVisible = !curve.IsVisible;
                    ApplySeriesVisibility();
                }
            });

            if (!isDesignMode)
            {
                RefreshTicketsChart();
                RefreshWarrantyAlertCountAsync().FireAndForget(nameof(RefreshWarrantyAlertCountAsync));
                RefreshDatabaseConnectionStatus();
            }
        }

        private RepairTicketsViewModel GetOrCreateRepairTicketsViewModel()
            => _repairTicketsViewModel ??= new RepairTicketsViewModel();

        private PartsControlViewModel GetOrCreatePartsControlViewModel()
            => _partsControlViewModel ??= new PartsControlViewModel();

        private InventoryViewModel GetOrCreateInventoryViewModel()
            => _inventoryViewModel ??= new InventoryViewModel();

        private BoneyardViewModel GetOrCreateBoneyardViewModel()
            => _boneyardViewModel ??= new BoneyardViewModel();

        private ReportsViewModel GetOrCreateReportsViewModel()
            => _reportsViewModel ??= new ReportsViewModel();

        private AlBarakaViewModel GetOrCreateAlBarakaViewModel()
            => _alBarakaViewModel ??= new AlBarakaViewModel();

        private void ShowUserProfile()
        {
            // If already open, close it (toggle behaviour)
            if (_userProfileWindow != null && _userProfileWindow.IsVisible)
            {
                _userProfileWindow.Close();
                _userProfileWindow = null;
                return;
            }

            _userProfileWindow = new UserProfileWindow(CurrentUser);
            _userProfileWindow.Closed += (_, _) => _userProfileWindow = null;

            // Position just below the title bar username button.
            // We'll position it in code-behind after the button click
            // provides screen coordinates. For now, position near top-right.
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                _userProfileWindow.Left = mainWindow.Left + mainWindow.ActualWidth
                                          - _userProfileWindow.Width - 60;
                _userProfileWindow.Top = mainWindow.Top + 38;
            }

            _userProfileWindow.Show();
        }

        private void Navigate(object viewModel, string successMessage)
        {
            CurrentView = viewModel;
            AppLogger.Success(successMessage);
        }

        private void RunGlobalSearch()
        {
            GlobalSearchResults.Clear();
            GlobalSearchSelectedIndex = -1;

            if (string.IsNullOrWhiteSpace(GlobalSearchQuery) || GlobalSearchQuery.Trim().Length < 2)
            {
                IsGlobalSearchOpen = false;
                return;
            }

            try
            {
                var results = _globalSearchService.Search(GlobalSearchQuery);
                foreach (var result in results)
                {
                    GlobalSearchResults.Add(result);
                }

                IsGlobalSearchOpen = GlobalSearchResults.Count > 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Global search failed: {ex.Message}");
                IsGlobalSearchOpen = false;
            }
        }

        private void NavigateToResult(GlobalSearchResult result)
        {
            GlobalSearchQuery = string.Empty;
            IsGlobalSearchOpen = false;
            GlobalSearchSelectedIndex = -1;

            switch (result.NavigationTarget)
            {
                case "Tickets":
                    ActiveSection = NavSection.RepairTickets;
                    var ticketVm = GetOrCreateRepairTicketsViewModel();
                    ticketVm.SearchQuery = result.Identifier;
                    Navigate(ticketVm, $"Found: {result.Identifier}");
                    break;

                case "Parts":
                    ActiveSection = NavSection.PartsControl;
                    Navigate(GetOrCreatePartsControlViewModel(), $"Parts: {result.Name}");
                    break;

                case "Inventory":
                    ActiveSection = NavSection.Inventory;
                    Navigate(GetOrCreateInventoryViewModel(), $"Inventory: {result.Name}");
                    break;

                case "Boneyard":
                    ActiveSection = NavSection.Boneyard;
                    Navigate(GetOrCreateBoneyardViewModel(), $"Boneyard: {result.Name}");
                    break;
            }
        }

        private void OnStatusRaised(string message, StatusLevel level)
        {
            SetStatus(message, level);
            _statusResetTimer.Stop();
            _statusResetTimer.Start();
        }

        private void SetStatus(string message, StatusLevel level)
        {
            StatusText = message;
            CurrentStatusLevel = level;
        }

        private void RequestAdminAccess()
        {
            try
            {
                if (IsAdminAccessGranted)
                {
                    return;
                }

                var login = new AdminLoginWindow { Owner = System.Windows.Application.Current.MainWindow };
                if (login.ShowDialog() != true || !login.IsAuthenticated)
                {
                    return;
                }

                IsAdminAccessGranted = true;
                AppLogger.Success("Admin access granted.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to validate admin access: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to validate admin access:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private bool EnsureAdminAccessGranted()
        {
            if (IsAdminAccessGranted)
            {
                return true;
            }

            AppLogger.Warning("Admin access is required.");
            System.Windows.MessageBox.Show(
                "Click 'Admin Access' first and log in to use administration tools.",
                "Admin Access Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            return false;
        }

        private void InitializeDatabase()
        {
            try
            {
                if (!EnsureAdminAccessGranted())
                {
                    return;
                }

                var initializer = new DatabaseInitializer(DatabasePathService.CurrentDatabasePath);
                var report = initializer.EnsureDatabaseReady();

                System.Windows.MessageBox.Show(
                    $"Database structure check completed for:\n{DatabasePathService.CurrentDatabasePath}\n\n{report}",
                    "Initialize Database",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
                AppLogger.Success("Database structure validated.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Database initialization failed: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Database initialization failed:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadDatabase()
        {
            try
            {
                if (!EnsureAdminAccessGranted())
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Database",
                    Filter = "SQLite Database (*.sql;*.db;*.sqlite)|*.sql;*.db;*.sqlite|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                DatabasePathService.SetDatabasePath(dialog.FileName);

                var initializer = new DatabaseInitializer(dialog.FileName);
                initializer.EnsureDatabaseReady();

                Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();

                System.Windows.MessageBox.Show(
                    $"Database loaded successfully and saved as default:\n{dialog.FileName}",
                    "Load Database",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                AppLogger.Success($"Database switched to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load database: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to load database:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ChangeAdminPassword()
        {
            try
            {
                var dialog = new ChangeAdminPasswordWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true && dialog.PasswordChanged)
                {
                    AppLogger.Success("Admin credentials changed successfully.");
                    System.Windows.MessageBox.Show(
                        "Admin credentials changed successfully.",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to change admin credentials: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to change admin credentials:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ResetSettings()
        {
            try
            {
                if (!EnsureAdminAccessGranted())
                {
                    return;
                }

                var confirm = System.Windows.MessageBox.Show(
                    "This will reset all app settings to default, including admin login and database selection. Continue?",
                    "Reset Settings",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (confirm != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }

                DatabasePathService.ResetToDefaultPath();
                UserPreferencesService.ResetToDefaultPreferences(DatabasePathService.DefaultPath);
                IsAdminAccessGranted = false;
                AutoGrantAdminAccess = false;
                LaunchAtWindowsStartup = false;

                Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
                RefreshTicketsChart();

                AppLogger.Success("All app settings were reset to default.");
                System.Windows.MessageBox.Show(
                    "All app settings were reset to default.\nAdmin credentials are now default: Admin / Admin.",
                    "Reset Completed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to reset settings: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to reset settings:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ResetDatabase()
        {
            try
            {
                if (!EnsureAdminAccessGranted())
                {
                    return;
                }

                var confirm = System.Windows.MessageBox.Show(
                    $"This will permanently delete the current database file:\n{DatabasePathService.CurrentDatabasePath}\n\nContinue?",
                    "Reset Database",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (confirm != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }

                var databasePath = DatabasePathService.CurrentDatabasePath;
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }

                var initializer = new DatabaseInitializer(databasePath);
                initializer.EnsureDatabaseReady();

                Dashboard.Refresh();
                TicketEvents.RaiseTicketsChanged();
                RefreshTicketsChart();

                AppLogger.Success("Database was recreated successfully.");
                System.Windows.MessageBox.Show(
                    "Old database was deleted and a new empty database was created.",
                    "Database Reset Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to reset database: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to reset database:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }


        private void OpenLogFolder()
        {
            try
            {
                var logDir = Core.FileLogger.LogDirectory;
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                {
                    AppLogger.Warning("Log folder not found or logging is disabled.");
                    return;
                }

                var todayLog = Core.FileLogger.TodayLogPath;
                if (todayLog != null && File.Exists(todayLog))
                {
                    // Open Explorer with today's log file selected
                    System.Diagnostics.Process.Start("explorer.exe",
                        $"/select,\"{todayLog}\"");
                }
                else
                {
                    // Just open the folder
                    System.Diagnostics.Process.Start("explorer.exe", logDir);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open log folder: {ex.Message}");
            }
        }

        private void BackupNow()
        {
            try
            {
                using var backupService = AppServices.Get<BackupService>();
                if (backupService.TryCreateBackupNow())
                {
                    LastBackupText = "just now";
                    AppLogger.Success("Manual backup created.");
                }
            }
            catch (Exception ex) { AppLogger.Error($"Backup failed: {ex.Message}"); }
        }

        private void RefreshAll()
        {
            Dashboard.Refresh();
            TicketEvents.RaiseTicketsChanged();
            RefreshTicketsChartAsync().FireAndForget(nameof(RefreshTicketsChartAsync));
            RefreshDatabaseConnectionStatusAsync().FireAndForget(nameof(RefreshDatabaseConnectionStatusAsync));
            RefreshDatabaseFileSizeAndBackupInfo();
            AppLogger.Success("Refreshed all data.");
        }

        private void RefreshDatabaseFileSizeAndBackupInfo()
        {
            try
            {
                var dbPath = DatabasePathService.CurrentDatabasePath;
                if (File.Exists(dbPath))
                {
                    var bytes = new FileInfo(dbPath).Length;
                    DatabaseFileSizeText = bytes >= 1_048_576
                        ? $"{bytes / 1_048_576.0:F1} MB"
                        : $"{bytes / 1024.0:F1} KB";
                }
                else
                {
                    DatabaseFileSizeText = "Not found";
                }

                var prefs = ESCenter.Services.UserPreferencesService.GetBackupLocation();
                var backupDir = string.IsNullOrWhiteSpace(prefs)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ESCenter Backups")
                    : prefs;

                if (Directory.Exists(backupDir))
                {
                    var dbName = Path.GetFileNameWithoutExtension(dbPath);
                    var ext = Path.GetExtension(dbPath);
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".bak";
                    var latest = Directory.GetFiles(backupDir, $"{dbName}_backup_*{ext}")
                                           .OrderByDescending(File.GetLastWriteTime)
                                           .FirstOrDefault();
                    if (latest != null)
                    {
                        var ago = DateTime.Now - File.GetLastWriteTime(latest);
                        LastBackupText = ago.TotalMinutes < 2 ? "just now"
                                       : ago.TotalHours < 1 ? $"{(int)ago.TotalMinutes}m ago"
                                       : ago.TotalHours < 24 ? $"{(int)ago.TotalHours}h ago"
                                                             : $"{(int)ago.TotalDays}d ago";
                    }
                    else LastBackupText = "No backups yet";
                }
                else LastBackupText = "No backups yet";
            }
            catch { DatabaseFileSizeText = "Error"; LastBackupText = "Error"; }
        }

        private void ShowWarrantySystem()
        {
            try
            {
                var window = new WarrantySystemWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                window.ShowDialog();
                AppLogger.Success("Warranty system opened.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open warranty system: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to open warranty system:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void RefreshTicketsChart() => RefreshTicketsChartAsync().FireAndForget(nameof(RefreshTicketsChartAsync));

        private async Task RefreshWarrantyAlertCountAsync()
        {
            var tickets = await _chartDataService.GetAllAsync();
            var expiring = WarrantyEvaluator.GetExpiringWarranties(tickets, 30);
            ExpiringWarrantyCount = expiring.Count;
        }

        private async Task RefreshTicketsChartAsync()
        {
            var tickets = await _chartDataService.GetAllAsync();

            var days = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .ToList();

            var openCounts = new List<int>();
            var finishedCounts = new List<int>();
            var criticalCounts = new List<int>();
            var majorCounts = new List<int>();
            var overdueCounts = new List<int>();

            foreach (var day in days)
            {
                var dayEnd = day.AddDays(1);
                openCounts.Add(tickets.Count(t => t.ReceiveDate >= day && t.ReceiveDate < dayEnd));
                finishedCounts.Add(tickets.Count(t => t.DeliveryDate.HasValue && t.DeliveryDate.Value >= day && t.DeliveryDate.Value < dayEnd));
                criticalCounts.Add(tickets.Count(t => string.Equals(t.PriorityLevel, "Critical", StringComparison.OrdinalIgnoreCase)
                                                   && !t.DeliveryDate.HasValue
                                                   && t.ReceiveDate < dayEnd));
                majorCounts.Add(tickets.Count(t => string.Equals(t.PriorityLevel, "Major", StringComparison.OrdinalIgnoreCase)
                                                && !t.DeliveryDate.HasValue
                                                && t.ReceiveDate < dayEnd));
                overdueCounts.Add(tickets.Count(t => !t.DeliveryDate.HasValue && (day - t.ReceiveDate.Date).TotalDays > 2));
            }

            EnsureStatusToggle("ongoing", "#FFFFFF");
            EnsureStatusToggle("Finished", "#5AA7FF");
            EnsureStatusToggle("Major", "#FFD54A");
            EnsureStatusToggle("Critical", "#FF8C42");
            EnsureStatusToggle("Overdue", "#FF5A5A");
            OrderStatusToggles("ongoing", "Finished", "Major", "Critical", "Overdue");

            TicketsSeries = new ISeries[]
            {
                BuildSeries("ongoing", openCounts, SKColor.Parse("#FFFFFF")),
                BuildSeries("Finished", finishedCounts, SKColor.Parse("#5AA7FF")),
                BuildSeries("Major", majorCounts, SKColor.Parse("#FFD54A")),
                BuildSeries("Critical", criticalCounts, SKColor.Parse("#FF8C42")),
                BuildSeries("Overdue", overdueCounts, SKColor.Parse("#FF5A5A"))
            };

            TicketsXAxis = new[]
            {
                new Axis
                {
                    Labels = days.Select(d => d.ToString("ddd")).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 10,
                    MinStep = 1,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(90, 167, 255, 100))
                }
            };

            TicketsYAxis = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 8,
                    Name = "Tickets",
                    NamePaint = new SolidColorPaint(SKColors.White),
                    NameTextSize = 11,
                    NamePadding = new LiveChartsCore.Drawing.Padding(0, 0, 0, 6),
                    MinStep = 1,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(90, 167, 255, 120))
                }
            };
        }

        private void RefreshDatabaseConnectionStatus() => RefreshDatabaseConnectionStatusAsync().FireAndForget(nameof(RefreshDatabaseConnectionStatusAsync));

        private async Task RefreshDatabaseConnectionStatusAsync()
        {
            var isOnline = await Task.Run(() =>
            {
                try
                {
                    var dbPath = DatabasePathService.CurrentDatabasePath;
                    if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                        return false;

                    using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                    conn.Open();
                    using var cmd = new SQLiteCommand("SELECT 1", conn);
                    _ = cmd.ExecuteScalar();
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            DatabaseConnectionStatusText = isOnline ? "Online" : "Offline";
            DatabaseConnectionBrush = isOnline
                ? System.Windows.Media.Brushes.LimeGreen
                : System.Windows.Media.Brushes.IndianRed;
        }

        private LineSeries<int> BuildSeries(string key, IReadOnlyCollection<int> values, SKColor color)
        {
            return new LineSeries<int>
            {
                Name = key,
                Values = values,
                GeometrySize = 7,
                LineSmoothness = 0.75,
                Stroke = new SolidColorPaint(color, 3),
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                Fill = null,
                IsVisible = IsSeriesVisible(key)
            };
        }

        private bool IsSeriesVisible(string key)
        {
            var existing = TicketStatusCurves.FirstOrDefault(c => c.Key == key);
            return existing?.IsVisible ?? true;
        }

        private void EnsureStatusToggle(string key, string colorHex)
        {
            if (TicketStatusCurves.Any(c => c.Key == key))
            {
                return;
            }

            var curve = new CurveSeries
            {
                Key = key,
                Stroke = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
                IsVisible = true
            };

            curve.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CurveSeries.IsVisible))
                {
                    ApplySeriesVisibility();
                }
            };

            TicketStatusCurves.Add(curve);
        }

        private void OrderStatusToggles(params string[] orderedKeys)
        {
            var orderedCurves = orderedKeys
                .Select(key => TicketStatusCurves.FirstOrDefault(curve => curve.Key == key))
                .Where(curve => curve is not null)
                .Cast<CurveSeries>()
                .ToList();

            if (orderedCurves.Count == 0)
            {
                return;
            }

            TicketStatusCurves.Clear();
            foreach (var curve in orderedCurves)
            {
                TicketStatusCurves.Add(curve);
            }
        }


        private void ApplySeriesVisibility()
        {
            foreach (var series in TicketsSeries.OfType<LineSeries<int>>())
            {
                series.IsVisible = IsSeriesVisible(series.Name ?? string.Empty);
            }

            OnPropertyChanged(nameof(TicketsSeries));
        }
        public enum NavSection
        {
            None,
            Dashboard,
            RepairTickets,
            PartsControl,
            Inventory,
            Boneyard
        }
    }
}
