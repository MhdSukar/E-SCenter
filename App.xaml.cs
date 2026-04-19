using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using ESCenter.Core;
using ESCenter.Data;
using ESCenter.Models;
using ESCenter.Services;
using ESCenter.Windows;

namespace ESCenter
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private NotifyIcon? _trayIcon;
        private TrayPopupWindow? _trayPopup;
        private BackupService? _backupService;
        private CancellationTokenSource? _pipeServerCts;

        protected override void OnStartup(StartupEventArgs e)
        {
            FileLogger.Initialize();

            const string appName = "ESCenterUniqueAppName";
            var createdNew = false;

            StartActivationPipeServer();

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                TryActivateExistingInstance();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            AppLogger.Success(
                $"E-SCenter started — v{System.Reflection.Assembly
                    .GetExecutingAssembly().GetName().Version}");

            AppServices.Build(services =>
            {
                services.AddSingleton<TicketsDataService>();
                services.AddSingleton<PartsRepository>();
                services.AddSingleton<InventoryRepository>();
                services.AddSingleton<BoneyardRepository>();
                services.AddSingleton<GlobalSearchService>();
                services.AddSingleton<BackupService>();
                services.AddSingleton<AlBarakaDataService>();
            });

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
            _backupService = AppServices.Get<BackupService>();
            _backupService.Start();
        }

        private void StartActivationPipeServer()
        {
            _pipeServerCts = new CancellationTokenSource();
            var token = _pipeServerCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream("ESCenterActivatePipe", PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                        await server.WaitForConnectionAsync(token);
                        using var reader = new StreamReader(server);
                        var message = await reader.ReadLineAsync();
                        if (string.Equals(message?.Trim(), "ACTIVATE", StringComparison.OrdinalIgnoreCase))
                        {
                            await Current.Dispatcher.InvokeAsync(ShowMainWindow);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                    }
                }
            }, token);
        }

        private void TryActivateExistingInstance()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", "ESCenterActivatePipe", PipeDirection.Out);
                client.Connect(500);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("ACTIVATE");
            }
            catch
            {
            }
        }

        public void ShowMainWindow()
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            if (MainWindow is ESCenter.MainWindow main)
            {
                main.ShowFromTray();
                return;
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
            _pipeServerCts?.Cancel();
            _pipeServerCts?.Dispose();

            Shutdown();
        }

        private void SetupTrayIcon()
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "Philips E-SCenter Icon.ico");

            _trayIcon = new NotifyIcon
            {
                Icon = File.Exists(iconPath) ? new System.Drawing.Icon(iconPath) : System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "E-SCenter Control Center"
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
            _trayIcon.MouseUp += (_, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    var cursorPos = Control.MousePosition;
                    ShowTrayPopupAt(cursorPos.X, cursorPos.Y);
                }
            };
        }

        private void ShowTrayPopupAt(int x, int y)
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
                _trayPopup.FadeAndHide();
                return;
            }

            _trayPopup.ShowWithAnimation();

            var left = x - _trayPopup.ActualWidth + 20;
            var top = y - _trayPopup.ActualHeight - 5;

            var screen = Screen.FromPoint(new System.Drawing.Point(x, y)).WorkingArea;
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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _backupService?.Dispose();
            _pipeServerCts?.Cancel();
            _pipeServerCts?.Dispose();

            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }

            AppLogger.Success("E-SCenter shutting down.");

            base.OnExit(e);
        }
    }
}
