using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ESCenter.Core;
using ESCenter.ViewModels;

namespace ESCenter.Windows
{
    public partial class PricingSystemWindow : Window
    {
        private bool _animatingClose;

        public PricingSystemWindow()
        {
            InitializeComponent();
            DataContext = new PricingSystemViewModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowFader.SlideIn(this);
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
                if (Content is UIElement c)
                {
                    c.RenderTransform = Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is PricingSystemViewModel vm && vm.SaveCommand.CanExecute(null))
                {
                    vm.SaveCommand.Execute(null);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex.Message);
                System.Windows.MessageBox.Show(ex.Message, "Pricing System", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
