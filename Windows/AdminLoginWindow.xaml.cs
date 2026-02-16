using System.Windows;
using System.Windows.Input;
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

            DialogResult = false;
            Close();
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
