using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ESCenter.Core;
using ESCenter.Services;
using ESCenter.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

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

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowRepairTicketsCommand { get; }
        public ICommand ShowPartsControlCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand ShowBoneyardCommand { get; }
        public ICommand ShowWarrantySystemCommand { get; }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _statusText = "System Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private Brush _statusBrush = Brushes.DeepSkyBlue;
        public Brush StatusBrush
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
        public Brush UsernameBrush { get; } = Brushes.DeepSkyBlue;

        public DashboardViewModel Dashboard { get; }

        private PlotModel _ticketsPlotModel;
        public PlotModel TicketsPlotModel
        {
            get => _ticketsPlotModel;
            set => SetProperty(ref _ticketsPlotModel, value);
        }

        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _statusResetTimer;

        public MainViewModel()
        {
            Dashboard = new DashboardViewModel();

            ShowDashboardCommand = new RelayCommand(_ => Navigate(Dashboard, "Dashboard loaded"));
            ShowRepairTicketsCommand = new RelayCommand(_ => Navigate(new RepairTicketsViewModel(), "Tickets loaded"));
            ShowPartsControlCommand = new RelayCommand(_ => Navigate(new PartsControlViewModel(), "Parts Control loaded"));
            ShowReportsCommand = new RelayCommand(_ => Navigate(new ReportsViewModel(), "Reports loaded"));
            ShowInventoryCommand = new RelayCommand(_ => Navigate(new InventoryViewModel(), "Inventory loaded"));
            ShowBoneyardCommand = new RelayCommand(_ => Navigate(new BoneyardViewModel(), "Boneyard loaded"));
            ShowWarrantySystemCommand = new RelayCommand(_ => ShowWarrantySystem());

            CurrentView = Dashboard;
            SetStatus("System Ready", StatusLevel.Info);

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => ClockText = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            _statusResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusResetTimer.Tick += (_, _) =>
            {
                _statusResetTimer.Stop();
                SetStatus("System Ready", StatusLevel.Info);
            };

            AppLogger.StatusRaised += OnStatusRaised;
            TicketEvents.TicketsChanged += (_, _) => RefreshTicketsChart();
            DatabasePathService.DatabasePathChanged += (_, _) => RefreshTicketsChart();

            _ticketsPlotModel = CreateTicketsActivityPlotModel();
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
                    StatusBrush = Brushes.LimeGreen;
                    StatusIcon = "\uE73E";
                    break;
                case StatusLevel.Warning:
                    StatusBrush = Brushes.Orange;
                    StatusIcon = "\uE7BA";
                    break;
                case StatusLevel.Error:
                    StatusBrush = Brushes.IndianRed;
                    StatusIcon = "\uEA39";
                    break;
                default:
                    StatusBrush = Brushes.DeepSkyBlue;
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
            TicketsPlotModel = CreateTicketsActivityPlotModel();
        }

        private PlotModel CreateTicketsActivityPlotModel()
        {
            var model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColor.FromAColor(80, OxyColors.White),
                TextColor = OxyColors.White,
                Padding = new OxyThickness(10, 8, 10, 0)
            };

            model.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                TextColor = OxyColors.White,
                LegendTitleColor = OxyColors.White,
                LegendFontSize = 9
            });

            var dataService = new TicketsDataService();
            var tickets = dataService.GetAll();

            var days = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .ToList();

            var openedSeries = new LineSeries
            {
                Title = "Opened",
                Color = OxyColor.Parse("#37E2D5"),
                StrokeThickness = 3,
                MarkerType = MarkerType.Star,
                MarkerSize = 3,
                MarkerFill = OxyColor.Parse("#37E2D5"),
                CanTrackerInterpolatePoints = false,
                TrackerFormatString = "{2}: {4:0} opened"
            };

            var finishedSeries = new AreaSeries
            {
                Title = "Finished",
                Color = OxyColor.Parse("#5AA7FF"),
                Fill = OxyColor.FromAColor(90, OxyColor.Parse("#5AA7FF")),
                StrokeThickness = 2,
                MarkerType = MarkerType.Star,
                TrackerFormatString = "{2}: {4:0} finished"
            };

            for (var i = 0; i < days.Count; i++)
            {
                var day = days[i];
                var dayEnd = day.AddDays(1);

                var opened = tickets.Count(t => t.ReceiveDate >= day && t.ReceiveDate < dayEnd);
                var finished = tickets.Count(t => t.DeliveryDate.HasValue &&
                                                  t.DeliveryDate.Value >= day &&
                                                  t.DeliveryDate.Value < dayEnd);

                openedSeries.Points.Add(new DataPoint(i, opened));
                finishedSeries.Points.Add(new DataPoint(i, finished));
                finishedSeries.Points2.Add(new DataPoint(i, 0));
            }

            model.Series.Add(finishedSeries);
            model.Series.Add(openedSeries);

            var xAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None
            };

            foreach (var d in days)
            {
                xAxis.Labels.Add(d.ToString("ddd"));
            }

            model.Axes.Add(xAxis);

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromAColor(30, OxyColors.White),
                MinorGridlineStyle = LineStyle.None,
                Title = "Tickets",
                TitleColor = OxyColors.White
            });

            return model;
        }
    }
}
