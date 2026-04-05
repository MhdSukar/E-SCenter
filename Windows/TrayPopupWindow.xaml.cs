using System.Windows;
using System.Windows.Media;
using ESCenter.Core;

namespace ESCenter.Windows
{
    public partial class TrayPopupWindow : Window
    {
        private bool _isHiding;

        public TrayPopupWindow()
        {
            InitializeComponent();
            Deactivated += (_, __) => FadeAndHide();
        }

        public void ShowWithAnimation()
        {
            _isHiding = false;
            Show();
            UpdateLayout();
            WindowFader.TrayPopupIn(this);
        }

        public void FadeAndHide()
        {
            if (_isHiding || !IsVisible)
            {
                return;
            }

            _isHiding = true;
            WindowFader.TrayPopupOut(this, () =>
            {
                Hide();
                if (Content is UIElement c)
                {
                    c.RenderTransform = Transform.Identity;
                }

                Opacity = 1;
                _isHiding = false;
            });
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ShowMainWindow();
            FadeAndHide();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ExitApp();
        }
    }
}
