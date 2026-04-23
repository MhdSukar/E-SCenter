using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ESCenter.Views
{
    public partial class TicketCustomerPanel : UserControl
    {
        public TicketCustomerPanel()
        {
            InitializeComponent();
        }

        public void FocusCustomerNameInput()
        {
            CustomerNameBox.Focus();
            Keyboard.Focus(CustomerNameBox);
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
