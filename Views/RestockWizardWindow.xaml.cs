using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;

namespace ESCenter.Views
{
    public partial class RestockWizardWindow : Window
    {
        private bool _animatingClose;
        private bool _wasMinimized;

        public RestockWizardWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += Window_Closing;
            StateChanged += Window_StateChanged;
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
                Hide();
                if (Content is UIElement content)
                {
                    content.RenderTransform = System.Windows.Media.Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowFader.FadeMinimize(this);
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                SetMaximizeButtonIcon("Maximize");
            }
            else
            {
                WindowState = WindowState.Maximized;
                SetMaximizeButtonIcon("Restore");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                btnMaximize_Click(sender, new RoutedEventArgs());
            }
            else
            {
                DragMove();
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _wasMinimized = true;
                return;
            }

            if (_wasMinimized && WindowState == WindowState.Normal)
            {
                _wasMinimized = false;
                WindowFader.FadeRestore(this);
            }
        }

        private void SetMaximizeButtonIcon(string resourceKey)
        {
            var img = new System.Windows.Controls.Image
            {
                Source = (System.Windows.Media.ImageSource)FindResource(resourceKey),
                Width = 14,
                Height = 14,
                Stretch = System.Windows.Media.Stretch.Uniform
            };

            btnMaximize.Content = img;
        }
    }
}
