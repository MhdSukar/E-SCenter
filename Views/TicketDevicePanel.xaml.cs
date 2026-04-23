using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ESCenter.Views
{
    public partial class TicketDevicePanel : UserControl
    {
        public TicketDevicePanel()
        {
            InitializeComponent();
        }

        private void SingleLineTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is FrameworkElement fe)
                {
                    fe.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }

                e.Handled = true;
            }
        }
    }
}
