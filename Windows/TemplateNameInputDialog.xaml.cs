using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ESCenter.Core;

namespace ESCenter.Windows
{
    public partial class TemplateNameInputDialog : Window
    {
        private bool _animatingClose;

        public string TemplateName { get; set; } = string.Empty;
        public bool Confirmed { get; private set; }

        public TemplateNameInputDialog()
        {
            InitializeComponent();
            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += Window_Closing;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (DialogResult.HasValue || _animatingClose)
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
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                AppLogger.Warning("Template name cannot be empty.");
                return;
            }

            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
