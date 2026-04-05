using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ESCenter.Core
{
    public static class WindowFader
    {
        private static readonly TimeSpan OpenDuration = TimeSpan.FromMilliseconds(220);
        private static readonly TimeSpan CloseDuration = TimeSpan.FromMilliseconds(160);

        public static void FadeIn(Window window, double durationMs = 180)
        {
            if (window == null)
            {
                return;
            }

            window.Opacity = 0;
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
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

            var animation = new DoubleAnimation(window.Opacity, 0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
                FillBehavior = FillBehavior.HoldEnd
            };
            animation.Completed += (_, _) => tcs.TrySetResult(true);
            window.BeginAnimation(UIElement.OpacityProperty, animation);
            return tcs.Task;
        }

        // Open: fade-in + slide up from +18px
        public static void SlideIn(Window window, TimeSpan? duration = null)
        {
            if (window == null)
            {
                return;
            }

            var dur = duration ?? OpenDuration;
            window.Opacity = 0;

            var content = window.Content as UIElement;
            if (content != null)
            {
                var transform = new TranslateTransform(0, 18);
                content.RenderTransform = transform;

                var slide = new DoubleAnimation(18, 0, new Duration(dur))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(TranslateTransform.YProperty, slide);
            }

            var fade = new DoubleAnimation(0, 1, new Duration(dur))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        // Close: fade-out + slide down slightly, then call onComplete
        public static void SlideOut(Window window, Action? onComplete, TimeSpan? duration = null)
        {
            if (window == null)
            {
                onComplete?.Invoke();
                return;
            }

            var dur = duration ?? CloseDuration;

            var content = window.Content as UIElement;
            if (content != null)
            {
                var transform = content.RenderTransform as TranslateTransform
                                ?? new TranslateTransform(0, 0);
                content.RenderTransform = transform;

                var slide = new DoubleAnimation(0, 12, new Duration(dur))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                transform.BeginAnimation(TranslateTransform.YProperty, slide);
            }

            var fade = new DoubleAnimation(window.Opacity, 0, new Duration(dur))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
                FillBehavior = FillBehavior.HoldEnd
            };
            fade.Completed += (_, __) => onComplete?.Invoke();
            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        // TrayPopup: scale + fade in from bottom-right
        public static void TrayPopupIn(Window window)
        {
            if (window == null)
            {
                return;
            }

            window.Opacity = 0;
            var content = window.Content as UIElement;
            if (content != null)
            {
                var scale = new ScaleTransform(0.92, 0.92);
                content.RenderTransformOrigin = new Point(1.0, 1.0);
                content.RenderTransform = scale;

                var sx = new DoubleAnimation(0.92, 1.0,
                    new Duration(TimeSpan.FromMilliseconds(180)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var sy = sx.Clone();
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            }

            var fade = new DoubleAnimation(0, 1,
                new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        // TrayPopup: scale + fade out
        public static void TrayPopupOut(Window window, Action? onComplete)
        {
            if (window == null)
            {
                onComplete?.Invoke();
                return;
            }

            var content = window.Content as UIElement;
            if (content != null)
            {
                var scale = content.RenderTransform as ScaleTransform
                            ?? new ScaleTransform(1, 1);
                content.RenderTransformOrigin = new Point(1.0, 1.0);
                content.RenderTransform = scale;

                var sx = new DoubleAnimation(1.0, 0.94,
                    new Duration(TimeSpan.FromMilliseconds(130)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var sy = sx.Clone();
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            }

            var fade = new DoubleAnimation(window.Opacity, 0,
                new Duration(TimeSpan.FromMilliseconds(130)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
                FillBehavior = FillBehavior.HoldEnd
            };
            fade.Completed += (_, __) => onComplete?.Invoke();
            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        public static void FadeMinimize(Window window)
        {
            if (window == null)
            {
                return;
            }

            var fade = new DoubleAnimation(0.9, 0, new Duration(TimeSpan.FromMilliseconds(120)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fade.Completed += (_, __) =>
            {
                window.WindowState = WindowState.Minimized;
                window.Opacity = 1;
            };

            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        public static void FadeRestore(Window window)
        {
            if (window == null)
            {
                return;
            }

            window.Opacity = 0;
            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        public static async Task AnimateViewTransitionAsync(ContentControl host)
        {
            if (host == null)
            {
                return;
            }

            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(110)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            host.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            await Task.Delay(110);

            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            host.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
    }
}
