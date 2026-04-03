using System.Windows;
using ESCenter.Core;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class AdminLoginWindow : Window
    {
        public bool IsAuthenticated { get; private set; }

        public AdminLoginWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
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
