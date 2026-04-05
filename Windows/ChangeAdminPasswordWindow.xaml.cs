using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ESCenter.Core;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class ChangeAdminPasswordWindow : Window
    {
        private bool _animatingClose;
        private bool _wasMinimized;

        public bool PasswordChanged { get; private set; }

        public ChangeAdminPasswordWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += Window_Closing;
            StateChanged += Window_StateChanged;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentUsernameBox.Text))
            {
                System.Windows.MessageBox.Show("Current username cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewUsernameBox.Text))
            {
                System.Windows.MessageBox.Show("New username cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                System.Windows.MessageBox.Show("New password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.Equals(NewPasswordBox.Password, ConfirmPasswordBox.Password))
            {
                System.Windows.MessageBox.Show("New password and confirm password do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var changed = UserPreferencesService.ChangeAdminCredentials(
                CurrentUsernameBox.Text,
                CurrentPasswordBox.Password,
                NewUsernameBox.Text,
                NewPasswordBox.Password);

            if (!changed)
            {
                System.Windows.MessageBox.Show("Current admin credentials are incorrect.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PasswordChanged = true;
            DialogResult = true;
            Close();
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
