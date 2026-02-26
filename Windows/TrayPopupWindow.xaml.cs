using System.Windows;

namespace ESCenter.Windows
{
    public partial class TrayPopupWindow : Window
    {
        public TrayPopupWindow()
        {
            InitializeComponent();
            Deactivated += (_, __) => Hide();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowMainWindow();
            Hide();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ExitApp();
        }
    }
}
