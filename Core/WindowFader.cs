using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace ESCenter.Core
{
    /// <summary>
    /// Central animation helper.  Every window in the app calls into here
    /// so all timings and easing functions are defined in exactly one place.
    /// </summary>
    public static class WindowFader
    {
        // ── Timing constants ────────────────────────────────────────────────
        public static readonly TimeSpan OpenDuration   = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan CloseDuration  = TimeSpan.FromMilliseconds(150);
        public static readonly TimeSpan RestoreDuration = TimeSpan.FromMilliseconds(220);
        public static readonly TimeSpan MinimizeDuration = TimeSpan.FromMilliseconds(130);

        // Easing shared by every animation
        private static readonly CubicEase EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        private static readonly CubicEase EaseIn  = new CubicEase { EasingMode = EasingMode.EaseIn  };

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>Fade a window in from 0 → 1.  Call on Loaded.</summary>
        public static void FadeIn(Window window, TimeSpan? duration = null)
        {
            window.Opacity = 0;
            var anim = MakeDoubleAnim(0, 1, duration ?? OpenDuration, EaseOut);
            window.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Fade a window out, then run <paramref name="onComplete"/>.
        /// Use when you need to hide/close after the animation finishes.
        /// </summary>
        public static void FadeOut(Window window, Action onComplete, TimeSpan? duration = null)
        {
            var anim = MakeDoubleAnim(window.Opacity, 0, duration ?? CloseDuration, EaseIn);
            anim.Completed += (_, __) => onComplete?.Invoke();
            window.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Fade from current opacity to 1 — used after restoring from minimise
        /// so the window doesn't just pop in.
        /// </summary>
        public static void FadeRestore(Window window)
        {
            var from = window.Opacity < 0.05 ? 0 : window.Opacity;
            var anim = MakeDoubleAnim(from, 1, RestoreDuration, EaseOut);
            window.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Fade out partially (to 0) before minimising, then immediately
        /// minimise (the OS will hide the window before the animation is seen
        /// on the taskbar, which is the natural Windows feel).
        /// </summary>
        public static void FadeMinimize(Window window)
        {
            var anim = MakeDoubleAnim(1, 0, MinimizeDuration, EaseIn);
            anim.Completed += (_, __) =>
            {
                window.WindowState = WindowState.Minimized;
                window.Opacity = 1; // reset so restore looks right
            };
            window.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Smooth view-switch animation on the ContentControl.
        /// Fades out (120 ms) then fades in (200 ms).
        /// Returns a Task so callers can await if needed.
        /// </summary>
        public static Task AnimateViewTransitionAsync(UIElement element)
        {
            var tcs = new TaskCompletionSource<bool>();

            var fadeOut = MakeDoubleAnim(1, 0, TimeSpan.FromMilliseconds(110), EaseIn);
            fadeOut.Completed += (_, __) =>
            {
                var fadeIn = MakeDoubleAnim(0, 1, TimeSpan.FromMilliseconds(200), EaseOut);
                fadeIn.Completed += (_2, __2) => tcs.TrySetResult(true);
                element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            return tcs.Task;
        }

        // ── Private ─────────────────────────────────────────────────────────
        private static DoubleAnimation MakeDoubleAnim(
            double from, double to, TimeSpan duration, IEasingFunction easing)
        {
            return new DoubleAnimation(from, to, new Duration(duration))
            {
                EasingFunction = easing,
                FillBehavior   = FillBehavior.HoldEnd
            };
        }
    }
}
