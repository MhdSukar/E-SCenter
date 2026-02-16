using System.Windows;
using System.Windows.Input;
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

            if (!UserPreferencesService.ChangeAdminPassword(CurrentPasswordBox.Password, NewPasswordBox.Password))
            {
                System.Windows.MessageBox.Show("Current password is incorrect.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
