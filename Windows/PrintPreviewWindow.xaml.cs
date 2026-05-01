using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly RepairTicket _ticket;
        private bool _animatingClose;

        public PrintPreviewWindow(RepairTicket ticket)
        {
            InitializeComponent();
            _ticket = ticket;
            Loaded += PrintPreviewWindow_Loaded;
            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += Window_Closing;
        }

        private void PrintPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DocViewer.Document = ReceiptDocumentBuilder.Build(_ticket);
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var service = AppServices.Get<ReceiptPrintService>();
            service.PrintReceipt(_ticket);
            AppLogger.Success("Ticket sent to printer.");
        }

        private void CopyTextButton_Click(object sender, RoutedEventArgs e)
        {
            var text = ReceiptDocumentBuilder.BuildPlainText(_ticket);
            Clipboard.SetText(text);
            AppLogger.Success("Receipt copied to clipboard.");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowFader.FadeMinimize(this);
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
                    c.RenderTransform = System.Windows.Media.Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
