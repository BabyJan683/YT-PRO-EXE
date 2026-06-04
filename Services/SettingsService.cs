using Newtonsoft.Json;
using YTDownloaderPro.Models;
using Microsoft.Win32;

namespace YTDownloaderPro.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public SettingsService(bool portableMode = false)
    {
        if (portableMode)
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _settingsPath = Path.Combine(exeDir, "settings.json");
        }
        else
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YTDownloaderPro");
            Directory.CreateDirectory(appData);
            _settingsPath = Path.Combine(appData, "settings.json");
        }

        _settings = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
        }
    }

    public void SetStartWithWindows(bool enable)
    {
        try
        {
            const string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var key = Registry.CurrentUser.OpenSubKey(regKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                key.SetValue("YTDownloaderPro", $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue("YTDownloaderPro", false);
            }
            _settings.StartWithWindows = enable;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Registry error: {ex.Message}");
        }
    }

    public bool IsStartWithWindowsEnabled()
    {
        try
        {
            const string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var key = Registry.CurrentUser.OpenSubKey(regKey, false);
            return key?.GetValue("YTDownloaderPro") != null;
        }
        catch { return false; }
    }

    public string GetDatabasePath()
    {
        if (!string.IsNullOrEmpty(_settings.DatabasePath))
            return _settings.DatabasePath;

        if (_settings.PortableMode)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "downloads.db");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YTDownloaderPro", "downloads.db");
    }
}
