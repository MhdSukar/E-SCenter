using System;
using System.Windows;
using ESCenter.Core;

namespace ESCenter.Windows
{
    public partial class TrayPopupWindow : Window
    {
        public TrayPopupWindow()
        {
            InitializeComponent();

            // Fade in every time the window becomes visible
            IsVisibleChanged += (_, args) =>
            {
                if ((bool)args.NewValue)
                    WindowFader.FadeIn(this, TimeSpan.FromMilliseconds(160));
            };

            // Fade out before hiding when focus is lost
            Deactivated += (_, __) => FadeAndHide();
        }

        private void FadeAndHide()
        {
            // Guard against double-trigger
            if (!IsVisible || Opacity < 0.01) return;

            WindowFader.FadeOut(this, () =>
            {
                Hide();
                Opacity = 1; // reset for next show
            }, TimeSpan.FromMilliseconds(120));
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide without animation delay so navigation feels instant
            Hide();
            Opacity = 1;
            ((App)System.Windows.Application.Current).ShowMainWindow();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ExitApp();
        }
    }
}
