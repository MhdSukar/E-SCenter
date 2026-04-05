using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace ESCenter.Core
{
    public static class WindowFader
    {
        public static void FadeIn(Window window, double durationMs = 180)
        {
            if (window == null)
            {
                return;
            }

            window.Opacity = 0;
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs));
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public static Task FadeOut(Window window, double durationMs = 160)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (window == null)
            {
                tcs.SetResult(true);
                return tcs.Task;
            }

            var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(durationMs));
            animation.Completed += (_, _) => tcs.TrySetResult(true);
            window.BeginAnimation(UIElement.OpacityProperty, animation);
            return tcs.Task;
        }
    }
}
