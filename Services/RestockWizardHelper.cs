using System.Windows;
using ESCenter.ViewModels;
using ESCenter.Views;

namespace ESCenter.Services
{
    public static class RestockWizardHelper
    {
        public static bool? Show(string itemName, string source, int currentQty)
        {
            if (System.Windows.Application.Current == null)
            {
                return false;
            }

            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                var vm = new RestockWizardViewModel(itemName, source, currentQty);
                var win = new RestockWizardWindow
                {
                    DataContext = vm,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                return win.ShowDialog();
            }

            return System.Windows.Application.Current.Dispatcher.Invoke(() => Show(itemName, source, currentQty));
        }

        public static void ShowIfNeeded(string itemName, string source, int newQty)
        {
            if (newQty > 0)
            {
                return;
            }

            Show(itemName, source, newQty);
        }
    }
}
