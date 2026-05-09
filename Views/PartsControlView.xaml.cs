using System.Windows.Controls;
using ESCenter.Models;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    public partial class PartsControlView : System.Windows.Controls.UserControl
    {
        public PartsControlView()
        {
            InitializeComponent();
        }

        private void PartsRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow row || DataContext is not PartsControlViewModel viewModel)
            {
                return;
            }

            if (row.Item is PartModel part)
            {
                viewModel.SelectedPart = part;
            }

            if (viewModel.EditPartCommand.CanExecute(null))
            {
                viewModel.EditPartCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
