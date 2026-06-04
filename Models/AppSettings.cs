namespace YTDownloaderPro.Models;

public class AppSettings
{
    public string DefaultDownloadPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "YTDownloaderPro");

    public string DefaultVideoQuality { get; set; } = "bestvideo+bestaudio/best";
    public string DefaultAudioFormat { get; set; } = "mp3";
    public string DefaultVideoFormat { get; set; } = "mp4";

    public int MaxConcurrentDownloads { get; set; } = 3;
    public int MaxThreadsPerDownload { get; set; } = 8;
    public int MaxConnectionsPerServer { get; set; } = 16;
    public int MaxRetriesOnError { get; set; } = 3;

    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool ShowSpeedInTray { get; set; } = true;

    public bool MonitorClipboard { get; set; } = true;
    public bool AutoDetectVideos { get; set; } = true;
    public bool ShowVideoPopup { get; set; } = true;

    public bool AutoUpdate { get; set; } = true;
    public bool CheckUpdateOnStartup { get; set; } = true;
    public string UpdateChannel { get; set; } = "stable";

    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "Red";

    public bool EnableScheduler { get; set; } = false;
    public string ScheduledStartTime { get; set; } = "02:00";
    public string ScheduledStopTime { get; set; } = "06:00";

    public bool UseProxy { get; set; } = false;
    public string ProxyAddress { get; set; } = string.Empty;
    public int ProxyPort { get; set; } = 8080;
    public string ProxyUsername { get; set; } = string.Empty;
    public string ProxyPassword { get; set; } = string.Empty;

    public bool LimitSpeed { get; set; } = false;
    public int SpeedLimitKBps { get; set; } = 1024;

    public bool SeparateVideoAudio { get; set; } = false;
    public bool EmbedThumbnail { get; set; } = true;
    public bool EmbedSubtitles { get; set; } = false;
    public bool WriteSubtitles { get; set; } = false;
    public string SubtitleLanguages { get; set; } = "en";

    public bool PortableMode { get; set; } = false;
    public string DatabasePath { get; set; } = string.Empty;

    public int SplitterPosition { get; set; } = 280;
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 750;
    public bool WindowMaximized { get; set; } = false;

    public List<string> QuickAccessPaths { get; set; } = new();
    public List<string> RecentUrls { get; set; } = new();

    public string YtDlpPath { get; set; } = "Resources\\yt-dlp.exe";
    public string Aria2Path { get; set; } = "Resources\\aria2c.exe";
    public string FfmpegPath { get; set; } = "Resources\\ffmpeg.exe";

    public bool UseCookies { get; set; } = false;
    public string CookiesFile { get; set; } = string.Empty;
    public string CookiesFromBrowser { get; set; } = string.Empty;

    // ─── License & Trial ─────────────────────────────────────────────────────
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = "none";   // none | trial | active | expired
    public DateTime? TrialStartDate { get; set; }

    // ─── Supabase (for online license validation) ─────────────────────────────
    public string SupabaseUrl { get; set; } = string.Empty;
    public string SupabaseAnonKey { get; set; } = string.Empty;
}
