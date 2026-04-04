using System.ComponentModel;
using System.Windows;
using ESCenter.Core;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class ChangeAdminPasswordWindow : Window
    {
        public bool PasswordChanged { get; private set; }

        private bool _animatingClose;

        public ChangeAdminPasswordWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
            Opacity = 0;
            Loaded  += (_, __) => WindowFader.FadeIn(this);
            Closing += Window_Closing;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_animatingClose) return;
            e.Cancel = true;
            _animatingClose = true;
            WindowFader.FadeOut(this, () => { _animatingClose = false; Close(); });
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentUsernameBox.Text))
            { MessageBox.Show("Current username cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (string.IsNullOrWhiteSpace(NewUsernameBox.Text))
            { MessageBox.Show("New username cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            { MessageBox.Show("New password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (!string.Equals(NewPasswordBox.Password, ConfirmPasswordBox.Password))
            { MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var changed = UserPreferencesService.ChangeAdminCredentials(
                CurrentUsernameBox.Text, CurrentPasswordBox.Password,
                NewUsernameBox.Text, NewPasswordBox.Password);

            if (!changed)
            { MessageBox.Show("Current credentials are incorrect.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            PasswordChanged = true;
            DialogResult    = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowFader.FadeMinimize(this);
        private void btnMaximize_Click(object sender, RoutedEventArgs e) => AdjustWindowSize();
        private void btnClose_Click(object sender, RoutedEventArgs e)    => Close();

        private void AdjustWindowSize()
        {
            if (WindowState == WindowState.Maximized)
            { WindowState = WindowState.Normal;    SetMaximizeButtonIcon("Maximize"); }
            else
            { WindowState = WindowState.Maximized; SetMaximizeButtonIcon("Restore"); }
        }

        private void SetMaximizeButtonIcon(string resourceKey)
        {
            btnMaximize.Content = new System.Windows.Controls.Image
            {
                Source  = (System.Windows.Media.ImageSource)FindResource(resourceKey),
                Width   = 14, Height = 14,
                Stretch = System.Windows.Media.Stretch.Uniform
            };
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) AdjustWindowSize();
            else DragMove();
        }
    }
}
