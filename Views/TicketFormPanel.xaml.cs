using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    public partial class TicketFormPanel : UserControl
    {
        public TicketFormPanel()
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

        private void SaveOrAddOnEnter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (DataContext is not RepairTicketsViewModel vm)
            {
                return;
            }

            if (vm.SelectedTicket != null && vm.SaveCommand.CanExecute(null))
            {
                vm.SaveCommand.Execute(null);
            }
            else if (vm.AddCommand.CanExecute(null))
            {
                vm.AddCommand.Execute(null);
            }

            e.Handled = true;
        }
    }
}
