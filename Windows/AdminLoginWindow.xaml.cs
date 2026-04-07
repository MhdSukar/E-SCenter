using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ESCenter.Core;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class AdminLoginWindow : Window
    {
        private bool _animatingClose;
        private bool _wasMinimized;

        public bool IsAuthenticated { get; private set; }

        public AdminLoginWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += Window_Closing;
            StateChanged += Window_StateChanged;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            // Allow immediate close for modal dialog outcomes (successful login or explicit cancel),
            // otherwise ShowDialog() can observe a null result and caller may interpret it as failure.
            if (DialogResult.HasValue)
            {
                return;
            }

            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            WindowFader.SlideOut(this, () =>
            {
                Hide();
                if (Content is UIElement c)
                {
                    c.RenderTransform = Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserPreferencesService.ValidateAdminCredentials(UsernameBox.Text, PasswordBox.Password))
            {
                IsAuthenticated = true;
                DialogResult = true;
                Close();
                AppLogger.Success("Welcome Back мн∂ ѕυкαя");
                return;
            }

            IsAuthenticated = false;
            UsernameBox.Clear();
            PasswordBox.Clear();
            ErrorMessageText.Visibility = Visibility.Visible;
            UsernameBox.Focus();
            AppLogger.Error("invalid Credentials");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowFader.FadeMinimize(this);
        }

        private void Window_StateChanged(object? sender, EventArgs e)
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

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
    }
}
