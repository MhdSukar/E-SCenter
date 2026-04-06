using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ESCenter.Core;
using ESCenter.Services;
using Microsoft.Win32;

namespace ESCenter
{
    public partial class MainWindow : Window
    {
        private readonly string _settingsPath;
        private bool _animatingClose;
        private int _closeAnimationVersion;
        private bool _wasMinimized;

        public MainWindow()
        {
            InitializeComponent();

            // Ctrl+F focuses the global search box from anywhere in the window
            KeyDown += (_, e) =>
            {
                if (e.Key == Key.F &&
                    (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    GlobalSearchBox.Focus();
                    GlobalSearchBox.SelectAll();
                    e.Handled = true;
                }
            };

            SetMaximizeButtonIcon("Maximize");
            Loaded += (_, __) =>
            {
                if (DataContext is ESCenter.ViewModels.MainViewModel vm)
                {
                    // If user chose to auto-grant admin access, respect that and do not prompt
                    if (ESCenter.Services.UserPreferencesService.GetAutoGrantAdminAccess())
                    {
                        vm.IsAdminAccessGranted = true;
                        vm.Dashboard.Refresh();
                    }
                    else
                    {
                        // Require admin access on startup
                        var login = new Windows.AdminLoginWindow { Owner = this };
                        if (login.ShowDialog() != true || !login.IsAuthenticated)
                        {
                            // terminate application if authentication fails or canceled
                            Environment.Exit(0);
                            return;
                        }

                        vm.IsAdminAccessGranted = true;
                        vm.Dashboard.Refresh();
                    }
                }
            };

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "ESCenter");
            _settingsPath = Path.Combine(dir, "windowsettings.json");

            SourceInitialized += MainWindow_SourceInitialized;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
            Loaded += (_, __) => WindowFader.SlideIn(this);

            var descriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
            descriptor?.AddValueChanged(MainContentHost, (_, _) => AnimateCurrentViewTransition());
        }

        private void AnimateCurrentViewTransition()
        {
            if (MainContentHost == null)
            {
                return;
            }

            if (MainContentHost.RenderTransform is not TransformGroup group ||
                group.Children.Count < 2 ||
                group.Children[0] is not ScaleTransform scale ||
                group.Children[1] is not TranslateTransform translate)
            {
                scale = new ScaleTransform(0.985, 0.985);
                translate = new TranslateTransform(0, 14);
                group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(translate);
                MainContentHost.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                MainContentHost.RenderTransform = group;
            }

            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(280)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slide = new DoubleAnimation(14, 0, new Duration(TimeSpan.FromMilliseconds(280)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleX = new DoubleAnimation(0.985, 1, new Duration(TimeSpan.FromMilliseconds(280)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleY = scaleX.Clone();

            MainContentHost.BeginAnimation(UIElement.OpacityProperty, fade);
            translate.BeginAnimation(TranslateTransform.YProperty, slide);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
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
                System.Windows.MessageBox.Show("Failed to save window settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            WindowFader.FadeMinimize(this);
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
                Source = (System.Windows.Media.ImageSource)FindResource(resourceKey),
                Width = 14,
                Height = 14,
                Stretch = System.Windows.Media.Stretch.Uniform
            };
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

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _wasMinimized = true;
                return;
            }

            if (_wasMinimized && WindowState == WindowState.Normal)
            {
                _wasMinimized = false;
                WindowFader.FadeRestore(this);
            }
        }

        public void ShowFromTray()
        {
            _closeAnimationVersion++;
            _animatingClose = false;

            BeginAnimation(UIElement.OpacityProperty, null);

            if (Content is UIElement content)
            {
                if (content.RenderTransform is TranslateTransform translateTransform)
                {
                    translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                }

                content.RenderTransform = Transform.Identity;
            }

            Opacity = 1;
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            var animationVersion = ++_closeAnimationVersion;

            WindowFader.SlideOut(this, () =>
            {
                if (animationVersion != _closeAnimationVersion)
                {
                    return;
                }

                Hide();
                if (Content is UIElement c)
                {
                    c.RenderTransform = Transform.Identity;
                }

                Opacity = 1;
                _animatingClose = false;
            });
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
