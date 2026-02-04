using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ESCenter.Core;
using ESCenter.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace ESCenter.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        // ================= Commands =================
        private ICommand _showDatabaseMGSCommand;
        public ICommand ShowDatabaseMGSCommand => _showDatabaseMGSCommand ??= new RelayCommand(_ => OpenDatabaseMGS());

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowRepairTicketsCommand { get; }
        public ICommand ShowPartsControlCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand ShowBoneyardCommand { get; }

        // ================= Properties =================
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

        private System.Windows.Media.Brush _statusBrush;
        public System.Windows.Media.Brush StatusBrush
        {
            get => _statusBrush;
            set => SetProperty(ref _statusBrush, value);
        }

        private string _statusIcon;
        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value);
        }

        private string _clockText;
        public string ClockText
        {
            get => _clockText;
            set => SetProperty(ref _clockText, value);
        }

        public string CurrentUser => "мн∂ ѕυкαя";
        public System.Windows.Media.Brush UsernameBrush { get; } = System.Windows.Media.Brushes.DeepSkyBlue;

        // ================= Dashboard =================
        public DashboardViewModel Dashboard { get; }

        // ================= OxyPlot Chart =================
        public PlotModel TicketsPlotModel { get; }

        // ================= Timers =================
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _statusResetTimer;

        public MainViewModel()
        {
            // ---------------- Dashboard ----------------
            Dashboard = new DashboardViewModel();

            ShowDashboardCommand = new RelayCommand(_ => Navigate(Dashboard, "Dashboard loaded"));
            ShowRepairTicketsCommand = new RelayCommand(_ => Navigate(new RepairTicketsViewModel(), "Tickets loaded"));
            ShowPartsControlCommand = new RelayCommand(_ => Navigate(new PartsControlViewModel(), "Parts Control loaded"));
            ShowReportsCommand = new RelayCommand(_ => Navigate(new ReportsViewModel(), "Reports loaded"));
            ShowInventoryCommand = new RelayCommand(_ => Navigate(new InventoryViewModel(), "Inventory loaded"));
            ShowBoneyardCommand = new RelayCommand(_ => Navigate(new BoneyardViewModel(), "Boneyard loaded"));

            // ---------------- Default View ----------------
            CurrentView = Dashboard;
            SetStatus("System Ready", StatusLevel.Info);

            // ---------------- Clock ----------------
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, __) => ClockText = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            // ---------------- Status Reset ----------------
            _statusResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusResetTimer.Tick += (_, __) =>
            {
                _statusResetTimer.Stop();
                SetStatus("System Ready", StatusLevel.Info);
            };

            AppLogger.StatusRaised += OnStatusRaised;

            // ---------------- Initialize Chart ----------------
            TicketsPlotModel = CreateTicketsScatterPlotModel();
        }

        // ================= Navigation =================
        private void Navigate(object viewModel, string successMessage)
        {
            CurrentView = viewModel;
            AppLogger.Success(successMessage);
        }

        // ================= Status =================
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

        private void OpenDatabaseMGS()
        {
            var login = new AdminLoginWindow();
            login.Owner = System.Windows.Application.Current.MainWindow;
            if (login.ShowDialog() == true && login.IsAuthenticated)
            {
                var dbWindow = new DatabaseManagementWindow();
                dbWindow.Owner = System.Windows.Application.Current.MainWindow;
                dbWindow.ShowDialog();
            }
        }

        // ================= Create Tickets Chart =================
        private PlotModel CreateTicketsScatterPlotModel()
        {
            var model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.White,
                TextColor = OxyColors.White   // ← global fallback
            };

            // -------- Legend --------
            var legend = new Legend
            {
                LegendPosition = LegendPosition.TopLeft,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendFontSize = 8,
                TextColor = OxyColors.White,
                LegendTitleColor = OxyColors.White
                //LegendBackground = OxyColors.Transparent,
                //LegendBorder = OxyColors.Transparent
            };
            model.Legends.Add(legend);

            // -------- Tickets --------
            var ticketsVM = new RepairTicketsViewModel();
            var tickets = ticketsVM.Tickets;

            var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu" };
            var dayIndex = days.Select((d, i) => new { d, i })
                               .ToDictionary(x => x.d, x => (double)x.i);

            var priorityColors = new Dictionary<string, OxyColor>
                {
                    { "Normal", OxyColors.SkyBlue },
                    { "Minor", OxyColors.Turquoise},
                    { "Major", OxyColors.Orange },
                    { "Critical", OxyColors.Red }
                };

            var seriesByPriority = new Dictionary<string, ScatterSeries>();

            foreach (var p in priorityColors)
            {
                var s = new ScatterSeries
                {
                    Title = p.Key,
                    MarkerType = MarkerType.Star,
                    MarkerFill = p.Value,
                    MarkerStroke = p.Value,
                    MarkerSize = 4
                };
                seriesByPriority[p.Key] = s;
                model.Series.Add(s);
            }

            var dayCounters = days.ToDictionary(d => d, d => 0);

            foreach (var ticket in tickets)
            {
                var day = ticket.ReceiveDate.DayOfWeek.ToString().Substring(0, 3);
                if (!dayIndex.ContainsKey(day))
                    continue;

                var priority = ticket.PriorityLevel ?? "Normal";
                if (!seriesByPriority.ContainsKey(priority))
                    priority = "Normal";

                dayCounters[day]++;
                seriesByPriority[priority].Points.Add(
                    new ScatterPoint(dayIndex[day], dayCounters[day])
                );
            }

            // -------- X Axis --------
            var xAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None
            };
            xAxis.Labels.AddRange(days);
            model.Axes.Add(xAxis);

            // -------- Y Axis --------
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                Title = "Tickets",
                TitleColor = OxyColors.White
            });

            return model;
        }

    }
}
