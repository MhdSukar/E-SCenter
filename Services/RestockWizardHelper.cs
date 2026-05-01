using System.Windows;
using ESCenter.ViewModels;
using ESCenter.Views;

namespace ESCenter.Services
{
    public static class RestockWizardHelper
    {
        public static void ShowIfNeeded(string itemName, string source, int newQty)
        {
            if (newQty > 0)
            {
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var vm = new RestockWizardViewModel(itemName, source, newQty);
                var win = new RestockWizardWindow
                {
                    DataContext = vm,
                    Owner = Application.Current.MainWindow
                };
                win.ShowDialog();
            });
        }
    }
}
