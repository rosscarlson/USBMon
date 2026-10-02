using System.Windows;
using System.Windows.Threading;
using USBMon.Capture;
using USBMon.Logging;
using USBMon.Models;
using USBMon.Settings;
using USBMon.Theming;

namespace USBMon;

public partial class App : Application
{
    private const string ShowEventName = @"Local\USBMon.Show";
    private const string ExitEventName = @"Local\USBMon.Exit";

    private static Mutex? _singleInstanceMutex;

    private MainWindow? _mainWindow;
    private TrayIconManager? _trayIcon;
    private DeviceWatcher? _watcher;
    private EventLogger? _logger;
    private AppSettings _settings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLog.Write("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
        };

        _singleInstanceMutex = new Mutex(initiallyOwned: true, @"Local\USBMon.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // The installer's uninstaller calls "USBMon.exe --exit" to ask the running
            // instance to shut down cleanly before taskkill; anything else just shows it.
            string eventName = e.Args.Contains("--exit", StringComparer.OrdinalIgnoreCase) ? ExitEventName : ShowEventName;
            try
            {
                using var handle = EventWaitHandle.OpenExisting(eventName);
                handle.Set();
            }
            catch
            {
                // Existing instance may not have created the handle yet; nothing more we can do.
            }
            Shutdown();
            return;
        }

        _settings = SettingsManager.Load();
        ThemeManager.Apply(_settings.Theme);

        _mainWindow = new MainWindow(_settings);
        MainWindow = _mainWindow;

        _logger = new EventLogger();
        _mainWindow.SetLogFilePath(_logger.LogFilePath);

        _trayIcon = new TrayIconManager();
        _trayIcon.OpenRequested += ShowMainWindow;
        _trayIcon.ThemeRequested += SetTheme;
        _trayIcon.CheckUpdatesRequested += CheckForUpdatesFromTray;
        _trayIcon.ExitRequested += ExitApplication;

        var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
        new Thread(() =>
        {
            var handles = new WaitHandle[] { showEvent, exitEvent };
            while (true)
            {
                int i = WaitHandle.WaitAny(handles);
                if (i == 0) Dispatcher.BeginInvoke(ShowMainWindow);
                else Dispatcher.BeginInvoke(ExitApplication);
            }
        })
        { IsBackground = true, Name = "USBMon control listener" }.Start();

        _watcher = new DeviceWatcher();
        _watcher.DeviceEventCaptured += OnDeviceEventCaptured;
        _watcher.Start();

        _ = DelayedStartupUpdateCheckAsync();
    }

    private async Task DelayedStartupUpdateCheckAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (_mainWindow != null) await _mainWindow.CheckForUpdatesAsync(manual: false);
        }
        catch (Exception ex)
        {
            ErrorLog.Write("DelayedStartupUpdateCheckAsync", ex);
        }
    }

    private void CheckForUpdatesFromTray()
    {
        ShowMainWindow();
        if (_mainWindow != null) _ = _mainWindow.CheckForUpdatesAsync(manual: true);
    }

    private void OnDeviceEventCaptured(DeviceRecord record)
    {
        _mainWindow?.AddRecord(record);
        _logger?.Log(record);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }
        _mainWindow.Activate();
    }

    public void SetTheme(ThemeMode mode)
    {
        _settings.Theme = mode;
        SettingsManager.Save(_settings);
        ThemeManager.Apply(mode);
        _trayIcon?.UpdateThemeChecks(mode);
    }

    public void ExitApplication()
    {
        if (_mainWindow != null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.SaveWindowSettings();
            _mainWindow.Close();
        }

        _watcher?.Dispose();
        _logger?.Dispose();
        _trayIcon?.Dispose();

        Shutdown();
    }
}
