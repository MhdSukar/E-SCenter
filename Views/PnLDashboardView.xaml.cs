using System.Windows;
using System.Windows.Controls;
using ESCenter.ViewModels;

namespace ESCenter.Views
{
    public partial class PnLDashboardView : UserControl
    {
        public PnLDashboardView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PnLDashboardViewModel vm)
            {
                await vm.LoadDataCommand.ExecuteAsync(null);
            }
        }
    }
}
