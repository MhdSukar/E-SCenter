using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;
using ESCenter.Windows;
using LiveChartsCore;
using LiveChartsCore.Measure;
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

        private bool _isAdminAccessGranted;
        public bool IsAdminAccessGranted
        {
            get => _isAdminAccessGranted;
            set => SetProperty(ref _isAdminAccessGranted, value);
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
        // Commands bound from MainWindow input bindings (F-keys)
        public ICommand OpenDashboardCommand { get; }
        public ICommand OpenRepairTicketsCommand { get; }
        public ICommand OpenPartsControlCommand { get; }
        public ICommand OpenInventoryCommand { get; }
        public ICommand OpenBoneyardCommand { get; }
        public ICommand OpenWarrantySystemCommand { get; }
        public ICommand ShowPartsControlCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowAlBarakaCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand ShowBoneyardCommand { get; }
        public ICommand ShowWarrantySystemCommand { get; }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private bool _isDashboardSelected = true;
        public bool IsDashboardSelected
        {
            get => _isDashboardSelected;
            set => SetProperty(ref _isDashboardSelected, value);
        }

        private bool _isRepairTicketsSelected;
        public bool IsRepairTicketsSelected
        {
            get => _isRepairTicketsSelected;
            set => SetProperty(ref _isRepairTicketsSelected, value);
        }

        private bool _isPartsControlSelected;
        public bool IsPartsControlSelected
        {
            get => _isPartsControlSelected;
            set => SetProperty(ref _isPartsControlSelected, value);
        }

        private bool _isInventorySelected;
        public bool IsInventorySelected
        {
            get => _isInventorySelected;
            set => SetProperty(ref _isInventorySelected, value);
        }

        private bool _isBoneyardSelected;
        public bool IsBoneyardSelected
        {
            get => _isBoneyardSelected;
            set => SetProperty(ref _isBoneyardSelected, value);
        }

        private string _statusText = "System Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private System.Windows.Media.Brush _statusBrush = System.Windows.Media.Brushes.DeepSkyBlue;
        public System.Windows.Media.Brush StatusBrush
        {
            get => _statusBrush;
            set => SetProperty(ref _statusBrush, value);
        }

        private string _statusIcon = "\uE946";
        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value);
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

        private decimal _totalAlBarakaSP;
        public decimal TotalAlBarakaSP
        {
            get => _totalAlBarakaSP;
            set => SetProperty(ref _totalAlBarakaSP, value);
        }

        private decimal _totalAlBarakaUSD;
        public decimal TotalAlBarakaUSD
        {
            get => _totalAlBarakaUSD;
            set => SetProperty(ref _totalAlBarakaUSD, value);
        }

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


        public ICommand ToggleCurveVisibilityCommand { get; }

        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _statusResetTimer;
        private RepairTicketsViewModel? _repairTicketsViewModel;

        public MainViewModel()
        {
            Dashboard = new DashboardViewModel();
            // Load persisted preference for auto-granting admin access on startup
            _autoGrantAdminAccess = ESCenter.Services.UserPreferencesService.GetAutoGrantAdminAccess();
            if (_autoGrantAdminAccess)
            {
                IsAdminAccessGranted = true;
            }

            _launchAtWindowsStartup = UserPreferencesService.GetLaunchAtWindowsStartup();
            var startupApplyResult = WindowsStartupService.SetLaunchAtStartup(_launchAtWindowsStartup);
            if (!startupApplyResult)
            {
                _launchAtWindowsStartup = WindowsStartupService.IsLaunchAtStartupEnabled();
                UserPreferencesService.SetLaunchAtWindowsStartup(_launchAtWindowsStartup);
            }

            ShowDashboardCommand = new RelayCommand(_ =>
            {
                // select dashboard and deselect others
                IsDashboardSelected = true;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = false;
                IsInventorySelected = false;
                IsBoneyardSelected = false;
                Navigate(Dashboard, "Dashboard loaded");
            });

            ShowRepairTicketsCommand = new RelayCommand(_ =>
            {
                IsDashboardSelected = false;
                IsRepairTicketsSelected = true;
                IsPartsControlSelected = false;
                IsInventorySelected = false;
                IsBoneyardSelected = false;
                Navigate(GetOrCreateRepairTicketsViewModel(), "Tickets loaded");
            });

            ShowPartsControlCommand = new RelayCommand(_ =>
            {
                IsDashboardSelected = false;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = true;
                IsInventorySelected = false;
                IsBoneyardSelected = false;
                Navigate(new PartsControlViewModel(), "Parts Control loaded");
            });

            ShowReportsCommand = new RelayCommand(_ =>
            {
                IsDashboardSelected = false;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = false;
                IsInventorySelected = false;
                IsBoneyardSelected = false;
                Navigate(new ReportsViewModel(), "Reports loaded");
            });

            ShowAlBarakaCommand = new RelayCommand(_ =>
            {
                // Open internal Al-Baraka view (replaces external launcher)
                IsDashboardSelected = false;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = false;
                IsInventorySelected = false;
                IsBoneyardSelected = false;
                Navigate(new AlBarakaViewModel(), "Al-Baraka loaded");
            });

            ShowInventoryCommand = new RelayCommand(_ =>
            {
                IsDashboardSelected = false;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = false;
                IsInventorySelected = true;
                IsBoneyardSelected = false;
                Navigate(new InventoryViewModel(), "Inventory loaded");
            });

            ShowBoneyardCommand = new RelayCommand(_ =>
            {
                IsDashboardSelected = false;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = false;
                IsInventorySelected = false;
                IsBoneyardSelected = true;
                Navigate(new BoneyardViewModel(), "Boneyard loaded");
            });

            ShowWarrantySystemCommand = new RelayCommand(_ =>
            {
                // warranty is a window - deselect all tabs
                IsDashboardSelected = false;
                IsRepairTicketsSelected = false;
                IsPartsControlSelected = false;
                IsInventorySelected = false;
                IsBoneyardSelected = false;
                ShowWarrantySystem();
            });

            // F-key bindings use these commands (declared separately so XAML can bind by name)
            OpenDashboardCommand = ShowDashboardCommand;
            OpenRepairTicketsCommand = ShowRepairTicketsCommand;
            OpenPartsControlCommand = ShowPartsControlCommand;
            OpenInventoryCommand = ShowInventoryCommand;
            OpenBoneyardCommand = ShowBoneyardCommand;
            OpenWarrantySystemCommand = ShowWarrantySystemCommand;

            CurrentView = Dashboard;
            SetStatus("System Ready", StatusLevel.Info);

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => ClockText = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            _statusResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _statusResetTimer.Tick += (_, _) =>
            {
                _statusResetTimer.Stop();
                SetStatus("System Ready", StatusLevel.Info);
            };

            AppLogger.StatusRaised += OnStatusRaised;
            TicketEvents.TicketsChanged += (_, _) => RefreshTicketsChart();
            DatabasePathService.DatabasePathChanged += (_, _) => RefreshTicketsChart();

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

            RefreshTicketsChart();

            // Load Al-Baraka totals for current month
            RefreshAlBarakaTotals();
        }

        public Task HandleDockControlCommandAsync(EscCommandRequest request)
        {
            var command = request.Command?.Trim().ToLowerInvariant();

            switch (command)
            {
                case "open_dashboard":
                    ShowDashboardCommand.Execute(null);
                    break;

                case "new_ticket":
                    ShowRepairTicketsCommand.Execute(null);
                    GetOrCreateRepairTicketsViewModel().PrepareNewTicketFromIntegration();
                    AppLogger.Success("DockControl: New ticket form ready.");
                    break;

                case "search_device":
                    ShowRepairTicketsCommand.Execute(null);
                    var search = TryGetParameter(request.Parameters, "query")
                                 ?? TryGetParameter(request.Parameters, "search")
                                 ?? TryGetParameter(request.Parameters, "device")
                                 ?? TryGetParameter(request.Parameters, "imei")
                                 ?? string.Empty;
                    GetOrCreateRepairTicketsViewModel().SearchDeviceFromIntegration(search);
                    AppLogger.Success($"DockControl: Device search applied ({search}).");
                    break;

                case "ready_pickups":
                    ShowRepairTicketsCommand.Execute(null);
                    GetOrCreateRepairTicketsViewModel().ShowReadyPickupsFromIntegration();
                    AppLogger.Success("DockControl: Ready pickups view applied.");
                    break;

                default:
                    AppLogger.Warning($"DockControl command not supported: {request.Command}");
                    break;
            }

            return Task.CompletedTask;
        }

        private RepairTicketsViewModel GetOrCreateRepairTicketsViewModel()
            => _repairTicketsViewModel ??= new RepairTicketsViewModel();

        private static string? TryGetParameter(Dictionary<string, string>? parameters, string key)
        {
            if (parameters == null)
            {
                return null;
            }

            return parameters.TryGetValue(key, out var value) ? value : null;
        }

        public void RefreshAlBarakaTotals(DateTime? start = null, DateTime? end = null)
        {
            try
            {
                var svc = new AlBarakaDataService();
                // default to current month when not specified
                if (!start.HasValue || !end.HasValue)
                {
                    var today = DateTime.Today;
                    start ??= new DateTime(today.Year, today.Month, 1);
                    end ??= start.Value.AddMonths(1).AddDays(-1);
                }

                var totals = svc.GetTotals(start, end);
                TotalAlBarakaSP = totals.TotalSP;
                TotalAlBarakaUSD = totals.TotalUSD;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to refresh Al-Baraka totals: {ex.Message}");
            }
        }

        private void Navigate(object viewModel, string successMessage)
        {
            CurrentView = viewModel;
            AppLogger.Success(successMessage);
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

            switch (level)
            {
                case StatusLevel.Success:
                    StatusBrush = System.Windows.Media.Brushes.LimeGreen;
                    StatusIcon = "\uE73E";
                    break;
                case StatusLevel.Warning:
                    StatusBrush = System.Windows.Media.Brushes.Orange;
                    StatusIcon = "\uE7BA";
                    break;
                case StatusLevel.Error:
                    StatusBrush = System.Windows.Media.Brushes.IndianRed;
                    StatusIcon = "\uEA39";
                    break;
                default:
                    StatusBrush = System.Windows.Media.Brushes.DeepSkyBlue;
                    StatusIcon = "\uE946";
                    break;
            }
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

        private void RefreshTicketsChart()
        {
            var dataService = new TicketsDataService();
            var tickets = dataService.GetAll();

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
                    MinStep = 1,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(90, 167, 255, 120))
                }
            };
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
    }
}
