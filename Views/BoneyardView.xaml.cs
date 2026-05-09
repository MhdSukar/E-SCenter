using System.Windows.Controls;
using ESCenter.Models;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    /// <summary>
    /// Interaction logic for BoneyardView.xaml
    /// </summary>
    public partial class BoneyardView : System.Windows.Controls.UserControl
    {
        public BoneyardView()
        {
            InitializeComponent();
        }

        private void BoneyardRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow row || DataContext is not BoneyardViewModel viewModel)
            {
                return;
            }

            if (row.Item is BoneyardModel device)
            {
                viewModel.SelectedDevice = device;
            }

            if (viewModel.EditCommand.CanExecute(null))
            {
                viewModel.EditCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
