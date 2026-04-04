using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ESCenter.Windows
{
    public partial class TrayPopupWindow : Window
    {
        private bool _isAnimating;

        public TrayPopupWindow()
        {
            InitializeComponent();
            Deactivated += async (_, __) => await HideAnimatedAsync();
        }

        public void ShowAnimated()
        {
            Opacity = 0;
            TraySlideTransform.Y = 8;
            if (!IsVisible)
            {
                Show();
            }

            Activate();

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(170)) { EasingFunction = easing };
            var slideIn = new DoubleAnimation(0, TimeSpan.FromMilliseconds(170)) { EasingFunction = easing };

            BeginAnimation(OpacityProperty, fadeIn);
            TraySlideTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        }

        public async Task HideAnimatedAsync()
        {
            if (!IsVisible || _isAnimating)
            {
                return;
            }

            _isAnimating = true;
            var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140)) { EasingFunction = easing };
            var slideOut = new DoubleAnimation(8, TimeSpan.FromMilliseconds(140)) { EasingFunction = easing };

            BeginAnimation(OpacityProperty, fadeOut);
            TraySlideTransform.BeginAnimation(TranslateTransform.YProperty, slideOut);

            await Task.Delay(160);
            Hide();
            _isAnimating = false;
        }

        private async void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ShowMainWindow();
            await HideAnimatedAsync();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ExitApp();
        }
    }
}
