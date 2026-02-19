using System.Windows;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class ChangeAdminPasswordWindow : Window
    {
        public bool PasswordChanged { get; private set; }

        public ChangeAdminPasswordWindow()
        {
            InitializeComponent();
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
