using System.Windows.Controls;
using ESCenter.Models;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    /// <summary>
    /// Interaction logic for Inventory.xaml
    /// </summary>
    public partial class InventoryView : System.Windows.Controls.UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
        }

        private void InventoryRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow row || DataContext is not InventoryViewModel viewModel)
            {
                return;
            }

            if (row.Item is InventoryItemModel item)
            {
                viewModel.SelectedItem = item;
            }

            if (viewModel.EditCommand.CanExecute(null))
            {
                viewModel.EditCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
