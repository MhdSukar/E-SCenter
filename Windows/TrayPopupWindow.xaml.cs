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
            ((App)System.Windows.Application.Current).ShowMainWindow();
            Hide();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ExitApp();
        }
    }
}
