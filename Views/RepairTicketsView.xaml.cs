using System.Windows;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
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
            TabCustomer.IsSelected = true;
            CustomerPanelRef.FocusCustomerNameInput();
        }
    }
}
