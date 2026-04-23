using System.Windows.Controls;
using System.Windows.Input;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    public partial class TicketInfoPanel : UserControl
    {
        public TicketInfoPanel()
        {
            InitializeComponent();
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
