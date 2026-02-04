using System.Windows;
using System.Windows.Controls;
using ESCenter.Core;

namespace ESCenter.Windows
{
    public partial class AdminLoginWindow : Window
    {
        public bool IsAuthenticated { get; private set; } = false;

        public AdminLoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameBox.Text == "Admin" && PasswordBox.Password == "Admin")
            {
                IsAuthenticated = true;
                this.DialogResult = true;
                this.Close();
                AppLogger.Success("Welcome Back мн∂ ѕυкαя");
            }
            else
            {
                this.Close();
                AppLogger.Error("invalid Credentials");
                //MessageBox.Show("Invalid credentials", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
