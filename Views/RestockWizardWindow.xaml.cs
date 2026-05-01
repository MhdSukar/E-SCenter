using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ESCenter.Core;

namespace ESCenter.Views
{
    public partial class RestockWizardWindow : Window
    {
        private bool _animatingClose;

        public RestockWizardWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");

            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += RestockWizardWindow_Closing;
        }

        private void RestockWizardWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            WindowFader.SlideOut(this, () =>
            {
                Closing -= RestockWizardWindow_Closing;
                Close();
            });
        }

        private void SetMaximizeButtonIcon(string resourceKey)
        {
            btnMaximize.Content = new System.Windows.Controls.Image
            {
                Source = (ImageSource)FindResource(resourceKey),
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform
            };
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void btnMaximize_Click(object sender, RoutedEventArgs e) => AdjustWindowSize();

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void AdjustWindowSize()
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

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                AdjustWindowSize();
            }
            else
            {
                DragMove();
            }
        }
    }
}
