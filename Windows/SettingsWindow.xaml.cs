using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using ESCenter.Core;

namespace ESCenter.Windows
{
    public partial class SettingsWindow : Window
    {
        private bool _animatingClose;
        private bool _wasMinimized;

        public SettingsWindow()
        {
            InitializeComponent();
            SetMaximizeButtonIcon("Maximize");
            Loaded += (_, __) => WindowFader.SlideIn(this);
            Closing += Window_Closing;
            StateChanged += Window_StateChanged;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (DialogResult.HasValue)
            {
                return;
            }

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

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowFader.FadeMinimize(this);
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

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            AdjustWindowSize();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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

        private void SetMaximizeButtonIcon(string resourceKey)
        {
            var img = new System.Windows.Controls.Image
            {
                Source = (System.Windows.Media.ImageSource)FindResource(resourceKey),
                Width = 14,
                Height = 14,
                Stretch = System.Windows.Media.Stretch.Uniform
            };

            if (this.FindName("btnMaximize") is System.Windows.Controls.Button btn)
            {
                btn.Content = img;
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

        private void BrowseBackupLocation_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.Description = "Select backup folder";
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (DataContext is ESCenter.ViewModels.SettingsViewModel vm)
            {
                vm.BackupLocation = dlg.SelectedPath;
            }
        }

        private void PositiveIntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            var proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.CaretIndex, e.Text);
            e.Handled = !IsPositiveIntegerText(proposedText);
        }

        private void PositiveIntegerTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            var text = e.DataObject.GetData(typeof(string)) as string;
            if (sender is not System.Windows.Controls.TextBox textBox || string.IsNullOrWhiteSpace(text))
            {
                e.CancelCommand();
                return;
            }

            var proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.CaretIndex, text);
            if (!IsPositiveIntegerText(proposedText))
            {
                e.CancelCommand();
            }
        }

        private static bool IsPositiveIntegerText(string text)
        {
            return int.TryParse(text, out var value) && value > 0;
        }
    }
}
