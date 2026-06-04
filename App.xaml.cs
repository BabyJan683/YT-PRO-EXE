using System.Windows;
using YTDownloaderPro.Services;
using YTDownloaderPro.ViewModels;
using YTDownloaderPro.Views;

namespace YTDownloaderPro;

public partial class App : Application
{
    private SettingsService? _settingsService;
    private DatabaseService? _db;
    private DownloadManager? _downloadManager;
    private ClipboardMonitorService? _clipboardMonitor;
    private UpdateService? _updateService;
    private SchedulerService? _scheduler;
    private BrowserExtensionServer? _browserServer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Handle global exceptions
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // Check for --minimized flag
        bool startMinimized = e.Args.Contains("--minimized");

        // Initialize services
        bool portable = e.Args.Contains("--portable") || File.Exists(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portable.txt"));

        _settingsService = new SettingsService(portable);
        var settings = _settingsService.Settings;

        if (startMinimized) settings.StartMinimized = true;

        // ─── License / Trial check ────────────────────────────────────────────
        var licService = new Services.LicenseService(settings);
        var licInfo = licService.GetLocalStatus();

        if (licInfo.State == Services.LicenseState.None)
        {
            // First run — start the 7-day trial automatically
            licService.StartTrial();
            _settingsService.Save();
        }
        else if (licInfo.State == Services.LicenseState.Expired)
        {
            // Trial / license expired — show activation dialog before main window
            var expiredMsg = MessageBox.Show(
                "Your 7-day trial of YT Downloader Pro has expired.\n\n" +
                "Please enter a license key in Settings → License to continue.\n\n" +
                "Press OK to open the app (activation required in Settings).",
                "Trial Expired — YT Downloader Pro",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (expiredMsg == MessageBoxResult.Cancel)
            {
                Shutdown();
                return;
            }
        }

        _db = new DatabaseService(_settingsService.GetDatabasePath());
        _downloadManager = new DownloadManager(settings, _db);
        _clipboardMonitor = new ClipboardMonitorService();
        _updateService = new UpdateService();
        _scheduler = new SchedulerService(_downloadManager, settings);
        _browserServer = new BrowserExtensionServer();

        var vm = new MainViewModel(
            _downloadManager,
            _settingsService,
            _clipboardMonitor,
            _updateService,
            _scheduler,
            _browserServer);

        var mainWindow = new MainWindow(vm, _settingsService, _clipboardMonitor, _browserServer);
        MainWindow = mainWindow;

        // Apply saved theme
        vm.ApplyTheme();

        mainWindow.Show();

        // Start scheduler
        _scheduler.Start();

        // Check for updates in background
        if (settings.CheckUpdateOnStartup && settings.AutoUpdate)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000); // Wait 5s after startup
                try
                {
                    var latestVersion = await _updateService.GetLatestYtDlpVersionAsync();
                    if (latestVersion != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            vm.StatusText = $"yt-dlp latest: {latestVersion}";
                        });
                    }
                }
                catch { }
            });
        }

        // Ensure resources folder exists with guidance
        EnsureResourcesExist();
    }

    private void EnsureResourcesExist()
    {
        var resourcesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        Directory.CreateDirectory(resourcesDir);

        var missing = new List<string>();
        if (!File.Exists(Path.Combine(resourcesDir, "yt-dlp.exe"))) missing.Add("yt-dlp.exe");
        if (!File.Exists(Path.Combine(resourcesDir, "aria2c.exe"))) missing.Add("aria2c.exe");
        if (!File.Exists(Path.Combine(resourcesDir, "ffmpeg.exe"))) missing.Add("ffmpeg.exe");

        if (missing.Count > 0 && _settingsService != null)
        {
            _ = Task.Run(async () =>
            {
                var updateSvc = new UpdateService();
                if (missing.Contains("yt-dlp.exe"))
                    await updateSvc.UpdateYtDlpAsync(
                        Path.Combine(resourcesDir, "yt-dlp.exe"));
                if (missing.Contains("aria2c.exe"))
                    await updateSvc.DownloadAria2Async(
                        Path.Combine(resourcesDir, "aria2c.exe"));
                if (missing.Contains("ffmpeg.exe"))
                    await updateSvc.DownloadFfmpegAsync(
                        Path.Combine(resourcesDir, "ffmpeg.exe"));
            });
        }
    }

    private void App_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
        var result = MessageBox.Show(
            $"YT Downloader Pro could not continue.\n\nError: {e.Exception.GetType().Name}\n{e.Exception.Message}\n\n" +
            $"A crash log was saved to:\n{GetCrashLogPath()}\n\nPlease send this file when reporting the issue.",
            "YT Downloader Pro — Error",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Error);

        e.Handled = true;
        if (result == MessageBoxResult.Cancel)
            Shutdown(1);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteCrashLog(ex);
    }

    private static string GetCrashLogPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YTDownloaderPro", "crash.log");
    }

    private static void WriteCrashLog(Exception ex)
    {
        try
        {
            var logPath = GetCrashLogPath();
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var content = $"[{DateTime.Now:O}]\n" +
                          $"Type: {ex.GetType().FullName}\n" +
                          $"Message: {ex.Message}\n" +
                          $"StackTrace:\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, content);
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboardMonitor?.Stop();
        _browserServer?.Stop();
        _scheduler?.Dispose();
        _db?.Dispose();
        base.OnExit(e);
    }
}
