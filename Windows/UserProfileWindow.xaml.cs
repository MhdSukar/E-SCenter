using System;
using System.ComponentModel;
using System.Windows;
using ESCenter.Core;

namespace ESCenter.Windows
{
    public partial class UserProfileWindow : Window
    {
        private bool _animatingClose;

        public UserProfileWindow(string username)
        {
            InitializeComponent();
            UsernameLabel.Text = username;
            AvatarInitial.Text = username.Length > 0
                ? username[0].ToString().ToUpper()
                : "U";

            Opacity = 0;
            Loaded += (_, __) => WindowFader.SlideIn(this,
                           TimeSpan.FromMilliseconds(180));
            Deactivated += (_, __) => CloseWithAnimation();
            Closing += Window_Closing;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            WindowFader.SlideOut(this, () =>
            {
                _animatingClose = false;
                Close();
            }, TimeSpan.FromMilliseconds(140));
        }

        private void CloseWithAnimation()
        {
            if (!IsVisible || _animatingClose)
            {
                return;
            }

            Close();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new SettingsWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    DataContext = new ESCenter.ViewModels.SettingsViewModel()
                };

                // Opening settings will deactivate this popup and trigger its close animation
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open Settings: {ex.Message}");
            }
        }
    }
}
