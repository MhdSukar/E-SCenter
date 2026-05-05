using System.Windows;
using System.Windows.Controls;

namespace ESCenter.Views
{
    public partial class AddTypeDefinitionWindow : Window
    {
        public string TypeNameValue { get; set; } = string.Empty;
        public string Unit1Value { get; set; } = string.Empty;
        public string Unit2Value { get; set; } = string.Empty;
        public Visibility PartsModeVisibility { get; }

        public AddTypeDefinitionWindow(bool isPartMode)
        {
            InitializeComponent();
            PartsModeVisibility = isPartMode ? Visibility.Visible : Visibility.Collapsed;
            DataContext = this;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TypeNameValue)) { System.Windows.MessageBox.Show("Type name is required."); return; }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
