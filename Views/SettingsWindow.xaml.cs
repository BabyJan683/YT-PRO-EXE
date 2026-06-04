using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using YTDownloaderPro.Services;

namespace YTDownloaderPro.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly LicenseService _licenseService;

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _licenseService = new LicenseService(settingsService.Settings);
        LoadSettings();
        LbNav.SelectedIndex = 0;
    }

    private void LoadSettings()
    {
        var s = _settingsService.Settings;
        ChkStartWindows.IsChecked = _settingsService.IsStartWithWindowsEnabled();
        ChkStartMinimized.IsChecked = s.StartMinimized;
        ChkMinimizeTray.IsChecked = s.MinimizeToTray;
        ChkCloseTray.IsChecked = s.CloseToTray;
        ChkNotifications.IsChecked = s.ShowNotifications;
        ChkClipboard.IsChecked = s.MonitorClipboard;
        ChkVideoPopup.IsChecked = s.ShowVideoPopup;
        ChkAutoUpdate.IsChecked = s.AutoUpdate;
        TxtDefaultPath.Text = s.DefaultDownloadPath;

        SldConcurrent.Value = s.MaxConcurrentDownloads;
        SldThreads.Value = s.MaxThreadsPerDownload;
        SldRetries.Value = s.MaxRetriesOnError;
        ChkLimitSpeed.IsChecked = s.LimitSpeed;
        TxtSpeedLimit.Text = s.SpeedLimitKBps.ToString();
        ChkEmbedThumbnail.IsChecked = s.EmbedThumbnail;
        ChkWriteSubtitles.IsChecked = s.WriteSubtitles;
        ChkEmbedSubtitles.IsChecked = s.EmbedSubtitles;
        TxtSubLangs.Text = s.SubtitleLanguages;

        ChkProxy.IsChecked = s.UseProxy;
        TxtProxyAddress.Text = s.ProxyAddress;
        TxtProxyPort.Text = s.ProxyPort.ToString();
        ChkCookies.IsChecked = s.UseCookies;
        TxtCookiesFile.Text = s.CookiesFile;

        RbDark.IsChecked = s.Theme == "Dark";
        RbLight.IsChecked = s.Theme == "Light";

        ChkSchedulerEnable.IsChecked = s.EnableScheduler;
        TxtScheduleStart.Text = s.ScheduledStartTime;
        TxtScheduleStop.Text = s.ScheduledStopTime;

        TxtYtDlpPath.Text = s.YtDlpPath;
        TxtAria2Path.Text = s.Aria2Path;
        TxtFfmpegPath.Text = s.FfmpegPath;
        ChkPortable.IsChecked = s.PortableMode;

        UpdateConditionalVisibility();
        LoadLicensePanel();
    }

    private void UpdateConditionalVisibility()
    {
        PnlSpeedLimit.Visibility = ChkLimitSpeed.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PnlProxy.Visibility = ChkProxy.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PnlCookies.Visibility = ChkCookies.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LbNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var panels = new[]
        {
            PanelGeneral, PanelDownloads, PanelConnections, PanelInterface,
            PanelScheduler, PanelBrowser, PanelAdvanced, PanelLicense, PanelAbout
        };
        foreach (var p in panels)
            p.Visibility = Visibility.Collapsed;

        var selected = panels.ElementAtOrDefault(LbNav.SelectedIndex);
        if (selected != null) selected.Visibility = Visibility.Visible;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings;

        s.StartMinimized = ChkStartMinimized.IsChecked == true;
        s.MinimizeToTray = ChkMinimizeTray.IsChecked == true;
        s.CloseToTray = ChkCloseTray.IsChecked == true;
        s.ShowNotifications = ChkNotifications.IsChecked == true;
        s.MonitorClipboard = ChkClipboard.IsChecked == true;
        s.ShowVideoPopup = ChkVideoPopup.IsChecked == true;
        s.AutoUpdate = ChkAutoUpdate.IsChecked == true;
        s.DefaultDownloadPath = TxtDefaultPath.Text;

        s.MaxConcurrentDownloads = (int)SldConcurrent.Value;
        s.MaxThreadsPerDownload = (int)SldThreads.Value;
        s.MaxRetriesOnError = (int)SldRetries.Value;
        s.LimitSpeed = ChkLimitSpeed.IsChecked == true;
        if (int.TryParse(TxtSpeedLimit.Text, out var spd)) s.SpeedLimitKBps = spd;
        s.EmbedThumbnail = ChkEmbedThumbnail.IsChecked == true;
        s.WriteSubtitles = ChkWriteSubtitles.IsChecked == true;
        s.EmbedSubtitles = ChkEmbedSubtitles.IsChecked == true;
        s.SubtitleLanguages = TxtSubLangs.Text;

        s.UseProxy = ChkProxy.IsChecked == true;
        s.ProxyAddress = TxtProxyAddress.Text;
        if (int.TryParse(TxtProxyPort.Text, out var port)) s.ProxyPort = port;
        s.UseCookies = ChkCookies.IsChecked == true;
        s.CookiesFile = TxtCookiesFile.Text;

        s.Theme = RbDark.IsChecked == true ? "Dark" : "Light";

        s.EnableScheduler = ChkSchedulerEnable.IsChecked == true;
        s.ScheduledStartTime = TxtScheduleStart.Text;
        s.ScheduledStopTime = TxtScheduleStop.Text;

        s.YtDlpPath = TxtYtDlpPath.Text;
        s.Aria2Path = TxtAria2Path.Text;
        s.FfmpegPath = TxtFfmpegPath.Text;
        s.PortableMode = ChkPortable.IsChecked == true;

        // Save Supabase config (if user filled them in the License panel)
        if (!string.IsNullOrWhiteSpace(TxtSupabaseUrl.Text))
            s.SupabaseUrl = TxtSupabaseUrl.Text.Trim();
        if (!string.IsNullOrWhiteSpace(TxtSupabaseKey.Text))
            s.SupabaseAnonKey = TxtSupabaseKey.Text.Trim();

        _settingsService.SetStartWithWindows(ChkStartWindows.IsChecked == true);
        _settingsService.Save();

        DialogResult = true;
        Close();
    }

    // ─── License Panel ─────────────────────────────────────────────────────

    private void LoadLicensePanel()
    {
        var info = _licenseService.GetLocalStatus();

        TxtPcName.Text = info.MachineName;
        TxtHardwareId.Text = info.HardwareId;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LicenseKey))
            TxtLicenseKey.Text = _settingsService.Settings.LicenseKey;

        TxtSupabaseUrl.Text = _settingsService.Settings.SupabaseUrl;
        TxtSupabaseKey.Text = _settingsService.Settings.SupabaseAnonKey;

        UpdateLicenseStatusUI(info);
    }

    private void UpdateLicenseStatusUI(LicenseInfo info)
    {
        switch (info.State)
        {
            case LicenseState.Active:
                LicIconStatus.Kind = PackIconKind.ShieldCheck;
                LicIconStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                TxtLicStatus.Text = "Licensed — Full Version";
                break;
            case LicenseState.Trial:
                LicIconStatus.Kind = PackIconKind.ShieldHalfFull;
                LicIconStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                TxtLicStatus.Text = $"Free Trial — {info.DaysRemaining} day(s) left";
                break;
            case LicenseState.Expired:
                LicIconStatus.Kind = PackIconKind.ShieldOff;
                LicIconStatus.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                TxtLicStatus.Text = "Trial Expired";
                break;
            default:
                LicIconStatus.Kind = PackIconKind.ShieldOff;
                LicIconStatus.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                TxtLicStatus.Text = "Not Activated";
                break;
        }
        TxtLicMessage.Text = info.Message;
    }

    public void NavigateToTab(int index)
    {
        LbNav.SelectedIndex = index;
    }

    private async void BtnActivate_Click(object sender, RoutedEventArgs e)
    {
        var key = TxtLicenseKey.Text.Trim();
        if (string.IsNullOrEmpty(key)) return;

        BtnActivate.IsEnabled = false;
        BtnActivate.Content = "Checking...";
        TxtActivateResult.Visibility = Visibility.Collapsed;

        // Save Supabase credentials first if changed
        if (!string.IsNullOrWhiteSpace(TxtSupabaseUrl.Text))
            _settingsService.Settings.SupabaseUrl = TxtSupabaseUrl.Text.Trim();
        if (!string.IsNullOrWhiteSpace(TxtSupabaseKey.Text))
            _settingsService.Settings.SupabaseAnonKey = TxtSupabaseKey.Text.Trim();

        var (success, message) = await _licenseService.ActivateKeyAsync(key);

        TxtActivateResult.Text = message;
        TxtActivateResult.Foreground = new SolidColorBrush(
            success ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtActivateResult.Visibility = Visibility.Visible;

        if (success)
        {
            _settingsService.Save();
            UpdateLicenseStatusUI(_licenseService.GetLocalStatus());
        }

        BtnActivate.IsEnabled = true;
        BtnActivate.Content = "Activate";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ChkLimitSpeed_Changed(object sender, RoutedEventArgs e) => UpdateConditionalVisibility();
    private void ChkProxy_Changed(object sender, RoutedEventArgs e) => UpdateConditionalVisibility();
    private void ChkCookies_Changed(object sender, RoutedEventArgs e) => UpdateConditionalVisibility();

    private void BtnBrowseDefault_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new VistaFolderBrowserDialog { SelectedPath = TxtDefaultPath.Text };
        if (dlg.ShowDialog(this) == true) TxtDefaultPath.Text = dlg.SelectedPath;
    }

    private void BtnBrowseCookies_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Cookies file|*.txt|All files|*.*" };
        if (dlg.ShowDialog(this) == true) TxtCookiesFile.Text = dlg.FileName;
    }

    private void BtnBrowseYtDlp_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "yt-dlp.exe|yt-dlp.exe|Exe files|*.exe" };
        if (dlg.ShowDialog(this) == true) TxtYtDlpPath.Text = dlg.FileName;
    }

    private void BtnUpdateYtDlp_Click(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            var svc = new UpdateService();
            var ok = await svc.UpdateYtDlpAsync(TxtYtDlpPath.Text);
            Dispatcher.Invoke(() =>
                MessageBox.Show(ok ? "yt-dlp updated successfully!" : "Update failed.",
                    "Update", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error));
        });
    }

    private void BtnBrowseAria2_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "aria2c.exe|aria2c.exe|Exe files|*.exe" };
        if (dlg.ShowDialog(this) == true) TxtAria2Path.Text = dlg.FileName;
    }

    private void BtnBrowseFfmpeg_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "ffmpeg.exe|ffmpeg.exe|Exe files|*.exe" };
        if (dlg.ShowDialog(this) == true) TxtFfmpegPath.Text = dlg.FileName;
    }

    private void BtnExtensionHelp_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Browser Extension Setup:\n\n" +
            "1. Open your browser extensions page\n" +
            "2. Enable Developer Mode\n" +
            "3. Load the 'Browser' folder as an unpacked extension\n" +
            "4. The extension will send URLs to this app on port 9614\n\n" +
            "Supports: Chrome, Edge, Brave, Firefox (with modifications)",
            "Browser Extension", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnYtDlpGithub_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/yt-dlp/yt-dlp") { UseShellExecute = true });

    private void BtnAria2Github_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/aria2/aria2") { UseShellExecute = true });
}
