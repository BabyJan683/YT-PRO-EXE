using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using YTDownloaderPro.Models;
using YTDownloaderPro.Services;
using YTDownloaderPro.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace YTDownloaderPro.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly SettingsService _settingsService;
    private readonly ClipboardMonitorService _clipboardMonitor;
    private readonly BrowserExtensionServer _browserServer;
    private TaskbarIcon? _trayIcon;
    private bool _isExiting;

    public MainWindow(MainViewModel vm, SettingsService settingsService,
        ClipboardMonitorService clipboardMonitor, BrowserExtensionServer browserServer)
    {
        InitializeComponent();
        _vm = vm;
        _settingsService = settingsService;
        _clipboardMonitor = clipboardMonitor;
        _browserServer = browserServer;
        DataContext = vm;

        SetupTrayIcon();
        vm.ApplyTheme();
    }

    // ─── Tray Icon Setup ─────────────────────────────────────────────────────

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "YT Downloader Pro",
            Visibility   = Visibility.Visible,
            IconSource   = LoadAppIcon()
        };

        // ── Context menu ──────────────────────────────────────────────────────
        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem
        {
            Header = "⏫  Open YT Downloader Pro",
            FontWeight = FontWeights.Bold
        };
        openItem.Click += (_, _) => ShowWindow();
        menu.Items.Add(openItem);

        var addItem = new System.Windows.Controls.MenuItem { Header = "＋  Add Download…" };
        addItem.Click += (_, _) => { ShowWindow(); ShowAddDownloadDialog(string.Empty); };
        menu.Items.Add(addItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var pauseAll  = new System.Windows.Controls.MenuItem { Header = "⏸  Pause All" };
        pauseAll.Click += (_, _) => _vm.PauseAllCommand.Execute(null);
        menu.Items.Add(pauseAll);

        var resumeAll = new System.Windows.Controls.MenuItem { Header = "▶  Resume All" };
        resumeAll.Click += (_, _) => _vm.ResumeAllCommand.Execute(null);
        menu.Items.Add(resumeAll);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙  Settings" };
        settingsItem.Click += (_, _) => { ShowWindow(); BtnSettings_Click(null!, null!); };
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "✕  Exit" };
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu        = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowWindow();
        _trayIcon.TrayLeftMouseDown    += (_, _) => ShowWindow();
    }

    /// <summary>
    /// Loads app.ico from the Resources folder next to the EXE.
    /// Falls back to the embedded application icon.
    /// </summary>
    private static BitmapImage? LoadAppIcon()
    {
        try
        {
            // First try: Resources\app.ico on disk (installer puts it there)
            var exeDir  = AppDomain.CurrentDomain.BaseDirectory;
            var icoPath = Path.Combine(exeDir, "Resources", "app.ico");
            if (File.Exists(icoPath))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource      = new Uri(icoPath, UriKind.Absolute);
                img.CacheOption    = BitmapCacheOption.OnLoad;
                img.DecodePixelWidth = 32;
                img.EndInit();
                img.Freeze();
                return img;
            }

            // Second try: embedded resource URI
            var uri = new Uri("pack://application:,,,/Resources/app.ico", UriKind.Absolute);
            var img2 = new BitmapImage();
            img2.BeginInit();
            img2.UriSource      = uri;
            img2.CacheOption    = BitmapCacheOption.OnLoad;
            img2.DecodePixelWidth = 32;
            img2.EndInit();
            img2.Freeze();
            return img2;
        }
        catch
        {
            return null;   // TaskbarIcon will use a default icon
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings;

        var screen = SystemParameters.WorkArea;
        Left   = Math.Max(0,   Math.Min(s.WindowLeft,   screen.Width  - 400));
        Top    = Math.Max(0,   Math.Min(s.WindowTop,    screen.Height - 300));
        Width  = Math.Max(900, Math.Min(s.WindowWidth,  screen.Width));
        Height = Math.Max(550, Math.Min(s.WindowHeight, screen.Height));

        if (s.WindowMaximized)
            WindowState = WindowState.Maximized;

        if (s.MonitorClipboard)
            _clipboardMonitor.Start(this);

        _browserServer.Start();

        TxtClipboardStatus.Text = s.MonitorClipboard ? "Clipboard: ON" : "Clipboard: OFF";
        TxtBrowserStatus.Text   = "Extension: ON";

        MenuStartWindows.IsChecked  = _settingsService.IsStartWithWindowsEnabled();
        MenuMinimizeTray.IsChecked  = s.MinimizeToTray;

        if (s.StartMinimized)
            WindowState = WindowState.Minimized;
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    public void ShowAddDownloadDialog(string url = "")
    {
        var dlg = new AddDownloadDialog(_vm, _settingsService.Settings, url) { Owner = this };
        dlg.ShowDialog();
    }

    public void ShowVideoDetectedPopup(string url, string title = "")
    {
        if (!_settingsService.Settings.ShowVideoPopup) return;

        // Bring window to front so popup is visible
        ShowWindow();

        var popup = new VideoDetectedPopup(url, title) { Owner = this };
        if (popup.ShowDialog() == true)
            ShowAddDownloadDialog(url);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsService.Settings.MinimizeToTray)
        {
            Hide();
            _trayIcon!.ShowBalloonTip(
                "YT Downloader Pro",
                "Running in system tray. Double-click to open.",
                BalloonIcon.Info);
        }
        else
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsService.Settings.CloseToTray && !_isExiting)
        {
            Hide();
            _trayIcon!.ShowBalloonTip(
                "YT Downloader Pro",
                "Still running in background. Right-click tray icon to exit.",
                BalloonIcon.Info);
        }
        else
        {
            ExitApp();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_settingsService.Settings.CloseToTray && !_isExiting)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            SaveWindowState();
            _clipboardMonitor.Stop();
            _browserServer.Stop();
            _trayIcon?.Dispose();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _settingsService.Settings.MinimizeToTray)
            Hide();
    }

    private void SaveWindowState()
    {
        var s = _settingsService.Settings;
        if (WindowState == WindowState.Normal)
        {
            s.WindowLeft   = Left;
            s.WindowTop    = Top;
            s.WindowWidth  = Width;
            s.WindowHeight = Height;
        }
        s.WindowMaximized = WindowState == WindowState.Maximized;
        _settingsService.Save();
    }

    private void ExitApp()
    {
        _isExiting = true;
        SaveWindowState();
        _clipboardMonitor.Stop();
        _browserServer.Stop();
        _trayIcon?.Dispose();
        Application.Current.Shutdown();
    }

    private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.AddDownloadCommand.Execute(null);
    }

    private void BtnBatchDownload_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new BatchDownloadDialog(_vm, _settingsService.Settings) { Owner = this };
        dlg.ShowDialog();
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_settingsService) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _vm.ApplyTheme();
            TxtClipboardStatus.Text = _settingsService.Settings.MonitorClipboard
                ? "Clipboard: ON" : "Clipboard: OFF";
            if (_settingsService.Settings.MonitorClipboard)
                _clipboardMonitor.Start(this);
            else
                _clipboardMonitor.Stop();
        }
    }

    private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        => _vm.ToggleTheme();

    private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UpdateDialog(_settingsService.Settings) { Owner = this };
        dlg.ShowDialog();
    }

    private void BtnOpenDownloadsFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = _settingsService.Settings.DefaultDownloadPath;
        Directory.CreateDirectory(path);
        System.Diagnostics.Process.Start("explorer.exe", path);
    }

    private void SidebarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tag)
            _vm.CurrentCategory = tag;
    }

    // ─── List view context menu ───────────────────────────────────────────────

    private void CmOpenFile_Click(object sender, RoutedEventArgs e)
        => _vm.OpenFileCommand.Execute(LvDownloads.SelectedItem);

    private void CmOpenFolder_Click(object sender, RoutedEventArgs e)
        => _vm.OpenFolderCommand.Execute(LvDownloads.SelectedItem);

    private void CmResume_Click(object sender, RoutedEventArgs e)
        => _vm.ResumeCommand.Execute(LvDownloads.SelectedItem);

    private void CmPause_Click(object sender, RoutedEventArgs e)
        => _vm.PauseCommand.Execute(LvDownloads.SelectedItem);

    private void CmStop_Click(object sender, RoutedEventArgs e)
        => _vm.StopCommand.Execute(LvDownloads.SelectedItem);

    private void CmCopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (LvDownloads.SelectedItem is DownloadItem item)
            Clipboard.SetText(item.Url);
    }

    private void CmRetry_Click(object sender, RoutedEventArgs e)
        => _vm.ResumeCommand.Execute(LvDownloads.SelectedItem);

    private void CmRemove_Click(object sender, RoutedEventArgs e)
        => _vm.RemoveCommand.Execute(LvDownloads.SelectedItem);

    private void CmRemoveDelete_Click(object sender, RoutedEventArgs e)
    {
        if (LvDownloads.SelectedItem is DownloadItem item)
        {
            var result = MessageBox.Show(
                $"Delete '{item.Title}' and its file permanently?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
                _vm.RemoveCommand.Execute(item);
        }
    }

    // ─── Menu bar ─────────────────────────────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e)         => ExitApp();
    private void MenuBatchDownload_Click(object sender, RoutedEventArgs e) => BtnBatchDownload_Click(sender, e);
    private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)    => BtnOpenDownloadsFolder_Click(sender, e);

    private void MenuRemoveFailed_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _vm.Downloads.Where(d => d.Status == DownloadStatus.Failed).ToList())
            _vm.RemoveCommand.Execute(item);
    }

    private void MenuExportList_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "Export Download List",
            Filter   = "Text File|*.txt|CSV|*.csv",
            FileName = "downloads"
        };
        if (dlg.ShowDialog() == true)
        {
            var lines = _vm.Downloads.Select(d =>
                $"{d.Title}\t{d.Url}\t{d.Status}\t{d.OutputPath}");
            File.WriteAllLines(dlg.FileName, lines);
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{dlg.FileName}\"");
        }
    }

    private void MenuLicense_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_settingsService) { Owner = this };
        dlg.Loaded += (_, _) => dlg.NavigateToTab(7);
        dlg.ShowDialog();
    }

    private void MenuStartWindows_Click(object sender, RoutedEventArgs e)
    {
        var enabled = MenuStartWindows.IsChecked;
        _settingsService.SetStartWithWindows(enabled);
        _settingsService.Settings.StartWithWindows = enabled;
        _settingsService.Save();
    }

    private void MenuMinimizeTray_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.MinimizeToTray = MenuMinimizeTray.IsChecked;
        _settingsService.Save();
    }

    private void MenuViewLog_Click(object sender, RoutedEventArgs e)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YTDownloaderPro", "crash.log");
        if (File.Exists(logPath))
            System.Diagnostics.Process.Start("notepad.exe", logPath);
        else
            MessageBox.Show("No crash log found. The app is running cleanly!",
                "View Log", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuInstallExtension_Click(object sender, RoutedEventArgs e)
    {
        var extDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Browser");
        if (Directory.Exists(extDir))
        {
            MessageBox.Show(
                "To install the browser extension:\n\n" +
                "1. Open Chrome / Edge  →  Extensions  (chrome://extensions)\n" +
                "2. Enable  'Developer mode'  (top-right toggle)\n" +
                "3. Click  'Load unpacked'\n" +
                $"4. Select the folder:\n   {extDir}\n\n" +
                "The extension automatically sends YouTube video URLs to YT Downloader Pro.",
                "Install Browser Extension",
                MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Process.Start("explorer.exe", extDir);
        }
        else
        {
            MessageBox.Show("Browser extension folder not found at:\n" + extDir,
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
        => BtnCheckUpdates_Click(sender, e);

    private void MenuVisitWebsite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/yt-dlp/yt-dlp",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        var licSvc = new Services.LicenseService(_settingsService.Settings);
        var info   = licSvc.GetLocalStatus();
        var licLine = info.State switch
        {
            Services.LicenseState.Active  => $"Licensed  —  Key: {info.Key}",
            Services.LicenseState.Trial   => $"Trial  —  {info.DaysRemaining} day(s) remaining",
            _                              => "Trial Expired"
        };

        MessageBox.Show(
            "YT Downloader Pro  v2.0\n\n" +
            "Powered by  yt-dlp  ·  aria2  ·  ffmpeg\n\n" +
            $"PC:     {info.MachineName}\n" +
            $"Status: {licLine}\n\n" +
            "© 2026 YT Downloader Pro",
            "About YT Downloader Pro",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
