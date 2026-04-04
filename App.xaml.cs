using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ESCenter.Models;
using ESCenter.Services;
using ESCenter.Windows;
using Forms = System.Windows.Forms;

namespace ESCenter
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private Forms.NotifyIcon? _trayIcon;
        private TrayPopupWindow? _trayPopup;
        private BackupService? _backupService;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "ESCenterUniqueAppName";
            var createdNew = false;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            InitializeDatabase();
            StartBackupService();
            SetupTrayIcon();
            ShowMainWindow();
        }

        private void InitializeDatabase()
        {
            try
            {
                var initializer = new DatabaseInitializer(DatabasePathService.CurrentDatabasePath);
                initializer.EnsureDatabaseReady();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Database initialization failed:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void StartBackupService()
        {
            _backupService = new BackupService();
            _backupService.Start();
        }

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

        public void ShowMainWindow()
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        public void ExitApp()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _backupService?.Dispose();

            Shutdown();
        }

        private void SetupTrayIcon()
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", "Philips E-SCenter Icon.ico");

            _trayIcon = new Forms.NotifyIcon
            {
                Icon = File.Exists(iconPath) ? new System.Drawing.Icon(iconPath) : System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "E-SCenter Control Center"
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
            _trayIcon.MouseUp += async (_, args) =>
            {
                if (args.Button == Forms.MouseButtons.Right)
                {
                    var cursorPos = Forms.Control.MousePosition;
                    await ShowTrayPopupAtAsync(cursorPos.X, cursorPos.Y);
                }
            };
        }

        private async Task ShowTrayPopupAtAsync(int x, int y)
        {
            if (MainWindow == null)
            {
                ShowMainWindow();
            }

            if (_trayPopup == null)
            {
                _trayPopup = new TrayPopupWindow();
            }

            _trayPopup.DataContext = MainWindow?.DataContext;

            if (_trayPopup.IsVisible)
            {
                await _trayPopup.HideAnimatedAsync();
                return;
            }

            _trayPopup.Opacity = 0;
            _trayPopup.Show();
            _trayPopup.UpdateLayout();

            var left = x - _trayPopup.ActualWidth + 20;
            var top = y - _trayPopup.ActualHeight - 5;

            var screen = Forms.Screen.FromPoint(new System.Drawing.Point(x, y)).WorkingArea;
            if (left < screen.Left)
            {
                left = screen.Left + 5;
            }

            if (top < screen.Top)
            {
                top = screen.Top + 5;
            }

            _trayPopup.Left = left;
            _trayPopup.Top = top;
            _trayPopup.ShowAnimated();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _backupService?.Dispose();

            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }

            base.OnExit(e);
        }
    }
}
