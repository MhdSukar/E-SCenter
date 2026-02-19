using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ESCenter
{
    public partial class MainWindow : Window
    {
        private readonly string _settingsPath;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                if (DataContext is ESCenter.ViewModels.MainViewModel vm)
                {
                    vm.Dashboard.Refresh();
                }
            };

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "ESCenter");
            _settingsPath = Path.Combine(dir, "windowsettings.json");

            SourceInitialized += MainWindow_SourceInitialized;
            Closing += MainWindow_Closing;

            var descriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
            descriptor?.AddValueChanged(MainContentHost, (_, _) => AnimateCurrentViewTransition());
        }

        private void AnimateCurrentViewTransition()
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(320)
            };

            animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });

            MainContentHost.BeginAnimation(OpacityProperty, animation);
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // If a saved settings file exists, restore bounds/state; otherwise center on the primary work area.
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var saved = JsonSerializer.Deserialize<WindowSettings>(json, opts);

                    if (saved != null &&
                        !double.IsNaN(saved.Width) && !double.IsNaN(saved.Height))
                    {
                        // Ensure we use Manual startup so we can set Left/Top explicitly
                        WindowStartupLocation = WindowStartupLocation.Manual;

                        // Clamp values so window is visible (handles monitor changes)
                        var left = Clamp(saved.Left, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Math.Max(100, saved.Width));
                        var top = Clamp(saved.Top, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Math.Max(100, saved.Height));

                        //Width = Math.Max(100, saved.Width);
                        Height = Math.Max(100, saved.Height);
                        Left = left;
                        Top = top;

                        // Apply state after bounds set
                        WindowState = saved.State;
                    }
                    else
                    {
                        CenterOnScreen();
                    }
                }
                catch
                {
                    // If any error reading/deserialize, fallback to center
                    CenterOnScreen();
                }
            }
            else
            {
                CenterOnScreen();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // When maximized, use RestoreBounds to get the normal window size and position.
                double left;
                double top;
                double width;
                double height;
                var stateToSave = WindowState;

                if (WindowState == WindowState.Normal)
                {
                    left = Left;
                    top = Top;
                    width = Width;
                    height = Height;
                }
                else
                {
                    // If Window is Maximized or Minimized, use RestoreBounds so normal position/size are preserved.
                    var rb = RestoreBounds;
                    left = rb.Left;
                    top = rb.Top;
                    width = rb.Width;
                    height = rb.Height;
                }

                var settings = new WindowSettings
                {
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    State = stateToSave
                };

                var dir = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // swallow exceptions on save to avoid blocking app close
                System.Windows.MessageBox.Show("Failed to save window settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CenterOnScreen()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var workArea = SystemParameters.WorkArea;
            // Use current Width/Height from XAML defaults (already set)
            Left = workArea.Left + (workArea.Width - Width) / 2;
            Top = workArea.Top + (workArea.Height - Height) / 2;
            WindowState = WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            AdjustWindowSize();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AdjustWindowSize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                btnMaximize.Content = "□";
            }
            else
            {
                WindowState = WindowState.Maximized;
                btnMaximize.Content = "❐";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                AdjustWindowSize();
            }
            else
            {
                DragMove();
            }
        }

        private static double Clamp(double value, double min, double max)
            => Math.Max(min, Math.Min(max, value));

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }

    // Small DTO persisted to disk
    internal class WindowSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowState State { get; set; } = WindowState.Normal;
    }
}
