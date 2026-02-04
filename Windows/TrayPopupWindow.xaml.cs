using System;
using System.Windows;
using ESCenter.ViewModels;

namespace ESCenter.Windows
{
    public partial class TrayPopupWindow : Window
    {
        public TrayPopupWindow()
        {
            InitializeComponent();

            // Auto-hide when clicking outside
            Deactivated += (_, __) => Hide();

            // Buttons wiring
            BtnDashboard.Click += (_, __) =>
            {
                ((App)System.Windows.Application.Current).ShowMainWindow();
                Hide();
            };

            BtnAddTicket.Click += (_, __) =>
            {
                // 1️⃣ Show main window
                var app = (App)System.Windows.Application.Current;
                app.ShowMainWindow();

                // 2️⃣ Switch to RepairTicketsView
                if (app.MainWindow.DataContext is MainViewModel vm)
                {
                    vm.ShowRepairTicketsCommand.Execute(null);
                }

                // 3️⃣ Hide the tray popup
                Hide();
            };

            BtnInventory.Click += (_, __) =>
            {
                // 1️⃣ Show main window
                var app = (App)System.Windows.Application.Current;
                app.ShowMainWindow();

                // 2️⃣ Switch to RepairTicketsView
                if (app.MainWindow.DataContext is MainViewModel vm)
                {
                    vm.ShowInventoryCommand.Execute(null);
                }

                // 3️⃣ Hide the tray popup
                Hide();
            };

            BtnExit.Click += (_, __) =>
            {
                ((App)System.Windows.Application.Current).ExitApp();
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
