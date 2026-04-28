using System.Windows;
using System.Windows.Input;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    public partial class CustomersView : System.Windows.Controls.UserControl
    {
        private CustomersViewModel? _subscribedViewModel;

        public CustomersView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.FocusFullNameRequested -= FocusFullName;
            }

            _subscribedViewModel = e.NewValue as CustomersViewModel;
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.FocusFullNameRequested += FocusFullName;
            }
        }

        private void FocusFullName()
        {
            FullNameBox.Focus();
            Keyboard.Focus(FullNameBox);
        }
    }
}
