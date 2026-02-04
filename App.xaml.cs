using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ESCenter.Windows;

namespace ESCenter
{
    public partial class App : System.Windows.Application
    {
        private static Mutex _mutex;
        private NotifyIcon _trayIcon;
        private TrayPopupWindow _trayPopup;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "ESCenterUniqueAppName";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            SetupTrayIcon();
            ShowMainWindow(); // remove if you want startup hidden
        }

        // ----------------------------
        // Single-instance handling
        // ----------------------------
        private void BringExistingInstanceToFront()
        {
            foreach (Window window in Current.Windows)
            {
                if (window is MainWindow main)
                {
                    main.WindowState = WindowState.Normal;
                    main.Show();
                    main.Activate();
                    break;
                }
            }
        }

        // ----------------------------
        // Main window handling
        // ----------------------------
        public void ShowMainWindow()
        {
            if (MainWindow == null)
                MainWindow = new MainWindow();

            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        public void ExitApp()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Shutdown();
        }

        // ----------------------------
        // Tray icon setup
        // ----------------------------
        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("Assets/app.ico"),
                Visible = true,
                Text = "E-SCenter"
            };

            _trayIcon.DoubleClick += (_, __) => ShowMainWindow();

            _trayIcon.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var cursorPos = Control.MousePosition;
                    ShowTrayPopupAt(cursorPos.X, cursorPos.Y);
                }
            };
        }

        private void ShowTrayPopupAt(int x, int y)
        {
            if (_trayPopup == null)
                _trayPopup = new TrayPopupWindow();

            if (_trayPopup.IsVisible)
            {
                _trayPopup.Hide();
                return;
            }

            _trayPopup.Opacity = 0;
            _trayPopup.Show();
            _trayPopup.UpdateLayout();
            _trayPopup.Opacity = 1;

            double left = x - _trayPopup.ActualWidth + 20;
            double top = y - _trayPopup.ActualHeight - 5;

            var screen = Screen.FromPoint(new System.Drawing.Point(x, y)).WorkingArea;
            if (left < screen.Left) left = screen.Left + 5;
            if (top < screen.Top) top = screen.Top + 5;

            _trayPopup.Left = left;
            _trayPopup.Top = top;

            _trayPopup.Activate();
        }
    }
}
