using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Services;

namespace ESCenter
{
    public partial class MainWindow : Window
    {
        private readonly string _settingsPath;
        private bool _closingToTray; // flag so OnClosing knows fade is already running

        public MainWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");

            // ── Fade in on startup ──────────────────────────────────────────
            Opacity = 0;
            Loaded += (_, __) =>
            {
                WindowFader.FadeIn(this);

                if (DataContext is ViewModels.MainViewModel vm)
                {
                    if (ESCenter.Services.UserPreferencesService.GetAutoGrantAdminAccess())
                    {
                        vm.IsAdminAccessGranted = true;
                        vm.Dashboard.Refresh();
                    }
                    else
                    {
                        var login = new Windows.AdminLoginWindow { Owner = this };
                        if (login.ShowDialog() != true || !login.IsAuthenticated)
                        {
                            System.Environment.Exit(0);
                            return;
                        }

                        vm.IsAdminAccessGranted = true;
                        vm.Dashboard.Refresh();
                    }
                }
            };

            // ── Restore-from-minimise fade ─────────────────────────────────
            StateChanged += (_, __) =>
            {
                if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
                    WindowFader.FadeRestore(this);
            };

            // ── Window settings ────────────────────────────────────────────
            var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            var dir     = System.IO.Path.Combine(appData, "ESCenter");
            _settingsPath = System.IO.Path.Combine(dir, "windowsettings.json");

            SourceInitialized += MainWindow_SourceInitialized;
            Closing           += MainWindow_Closing;

            // ── View-switch animation (improved via WindowFader) ───────────
            var descriptor = DependencyPropertyDescriptor
                .FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
            descriptor?.AddValueChanged(MainContentHost, (_, __) => AnimateViewSwitch());
        }

        // ── View transition ────────────────────────────────────────────────
        private void AnimateViewSwitch()
            => _ = WindowFader.AnimateViewTransitionAsync(MainContentHost);

        // ── Window settings persistence ────────────────────────────────────
        private void MainWindow_SourceInitialized(object? sender, System.EventArgs e)
        {
            if (!File.Exists(_settingsPath)) { CenterOnScreen(); return; }

            try
            {
                var json  = File.ReadAllText(_settingsPath);
                var opts  = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var saved = JsonSerializer.Deserialize<WindowSettings>(json, opts);

                if (saved != null && !double.IsNaN(saved.Width) && !double.IsNaN(saved.Height))
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    var left = Clamp(saved.Left,
                        SystemParameters.VirtualScreenLeft,
                        SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - System.Math.Max(100, saved.Width));
                    var top = Clamp(saved.Top,
                        SystemParameters.VirtualScreenTop,
                        SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - System.Math.Max(100, saved.Height));

                    Height      = System.Math.Max(100, saved.Height);
                    Left        = left;
                    Top         = top;
                    WindowState = saved.State;
                }
                else
                {
                    CenterOnScreen();
                }
            }
            catch
            {
                CenterOnScreen();
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                double left, top, width, height;
                var stateToSave = WindowState;

                if (WindowState == WindowState.Normal)
                {
                    left = Left; top = Top; width = Width; height = Height;
                }
                else
                {
                    var rb = RestoreBounds;
                    left = rb.Left; top = rb.Top; width = rb.Width; height = rb.Height;
                }

                var settings = new WindowSettings
                {
                    Left = left, Top = top, Width = width, Height = height, State = stateToSave
                };

                var dir = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                System.Windows.MessageBox.Show("Failed to save window settings.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Title bar controls ─────────────────────────────────────────────
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowFader.FadeMinimize(this);

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
            => AdjustWindowSize();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();       // OnClosing intercepts and hides to tray with fade

        private void AdjustWindowSize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                SetMaximizeButtonIcon("Maximize");
            }
            else
            {
                WindowState = WindowState.Maximized;
                SetMaximizeButtonIcon("Restore");
            }
        }

        private void SetMaximizeButtonIcon(string resourceKey)
        {
            btnMaximize.Content = new System.Windows.Controls.Image
            {
                Source  = (System.Windows.Media.ImageSource)FindResource(resourceKey),
                Width   = 14,
                Height  = 14,
                Stretch = System.Windows.Media.Stretch.Uniform
            };
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) AdjustWindowSize();
            else DragMove();
        }

        private void CenterOnScreen()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var workArea = SystemParameters.WorkArea;
            Left  = workArea.Left + (workArea.Width  - Width)  / 2;
            Top   = workArea.Top  + (workArea.Height - Height) / 2;
            WindowState = WindowState.Normal;
        }

        private static double Clamp(double value, double min, double max)
            => System.Math.Max(min, System.Math.Min(max, value));

        // ── Hide to tray with fade instead of instant disappear ────────────
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_closingToTray) { base.OnClosing(e); return; }

            e.Cancel = true;
            WindowFader.FadeOut(this, () =>
            {
                Hide();
                Opacity = 1; // reset so next Show() starts correctly
            });
        }
    }

    internal class WindowSettings
    {
        public double      Left   { get; set; }
        public double      Top    { get; set; }
        public double      Width  { get; set; }
        public double      Height { get; set; }
        public WindowState State  { get; set; } = WindowState.Normal;
    }
}
