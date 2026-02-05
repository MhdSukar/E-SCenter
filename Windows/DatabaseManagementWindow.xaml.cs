using System;
using System.Windows;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class DatabaseManagementWindow : Window
    {
        private readonly DatabaseInitializer _databaseInitializer;

        public DatabaseManagementWindow()
        {
            InitializeComponent();
            _databaseInitializer = new DatabaseInitializer(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void ReceiveDateabases_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _databaseInitializer.EnsureDatabaseReady();
                System.Windows.MessageBox.Show("Database and tables verified/created successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Database operation failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
