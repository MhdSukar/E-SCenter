using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    /// <summary>
    /// Interaction logic for CustomersView.xaml
    /// </summary>
    public partial class RepairTicketsView : System.Windows.Controls.UserControl
    {
        private RepairTicketsViewModel? _subscribedViewModel;

        public RepairTicketsView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.FocusCustomerNameRequested -= FocusCustomerName;
            }

            _subscribedViewModel = e.NewValue as RepairTicketsViewModel;
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.FocusCustomerNameRequested += FocusCustomerName;
            }
        }

        private void FocusCustomerName()
        {
            CustomerNameBox.Focus();
            Keyboard.Focus(CustomerNameBox);
        }

        private void SingleLineTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void SaveOrAddOnEnter(object sender, System.Windows.Input.KeyEventArgs e)
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
