using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ESCenter.Services;
using Microsoft.Win32;

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
                        WindowStartupLocation = WindowStartupLocation.Manual;

                        var left = Clamp(saved.Left, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Math.Max(100, saved.Width));
                        var top = Clamp(saved.Top, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Math.Max(100, saved.Height));

                        Height = Math.Max(100, saved.Height);
                        Left = left;
                        Top = top;
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
            else
            {
                CenterOnScreen();
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
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
                    Directory.CreateDirectory(dir!);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                MessageBox.Show("Failed to save window settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CenterOnScreen()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var workArea = SystemParameters.WorkArea;
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

        private void AlBarakaButton_Click(object sender, RoutedEventArgs e)
        {
            var preferences = UserPreferencesService.Load();
            var configuredPath = preferences.AlBarakaExecutablePath?.Trim() ?? string.Empty;

            if (TryLaunchAlBaraka(configuredPath))
            {
                return;
            }

            var selectedPath = PromptForAlBarakaPath();
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            preferences.AlBarakaExecutablePath = selectedPath;
            UserPreferencesService.Save(preferences);

            if (!TryLaunchAlBaraka(selectedPath))
            {
                MessageBox.Show(
                    "The selected Al-Baraka file could not be opened. Please choose a valid executable or shortcut.",
                    "Al-Baraka",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static bool TryLaunchAlBaraka(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string? PromptForAlBarakaPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Al-Baraka executable or shortcut",
                Filter = "Applications (*.exe;*.lnk)|*.exe;*.lnk|Executable (*.exe)|*.exe|Shortcut (*.lnk)|*.lnk|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            var result = dialog.ShowDialog(this);
            return result == true ? dialog.FileName : null;
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

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }

    internal class WindowSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowState State { get; set; } = WindowState.Normal;
    }
}
