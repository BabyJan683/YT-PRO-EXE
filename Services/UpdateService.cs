using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;

namespace YTDownloaderPro.Services;

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsMandatory { get; set; }
}

public class UpdateService
{
    private readonly HttpClient _http;
    private const string UpdateCheckUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
    private const string YtDlpReleasesUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";

    public UpdateService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "YTDownloaderPro/2.0");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<string?> GetLatestYtDlpVersionAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(YtDlpReleasesUrl, ct);
            dynamic? release = JsonConvert.DeserializeObject(json);
            return release?.tag_name;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateYtDlpAsync(string ytDlpPath, CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(YtDlpReleasesUrl, ct);
            dynamic? release = JsonConvert.DeserializeObject(json);
            if (release?.assets == null) return false;

            string? downloadUrl = null;
            foreach (var asset in release.assets)
            {
                string name = asset.name?.ToString() ?? string.Empty;
                if (name == "yt-dlp.exe")
                {
                    downloadUrl = asset.browser_download_url?.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl)) return false;

            var backupPath = ytDlpPath + ".bak";
            if (File.Exists(ytDlpPath))
                File.Move(ytDlpPath, backupPath, true);

            var data = await _http.GetByteArrayAsync(downloadUrl, ct);
            await File.WriteAllBytesAsync(ytDlpPath, data, ct);

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DownloadAria2Async(string aria2Path, CancellationToken ct = default)
    {
        try
        {
            const string url = "https://api.github.com/repos/aria2/aria2/releases/latest";
            var json = await _http.GetStringAsync(url, ct);
            dynamic? release = JsonConvert.DeserializeObject(json);
            if (release?.assets == null) return false;

            string? downloadUrl = null;
            foreach (var asset in release.assets)
            {
                string name = asset.name?.ToString() ?? string.Empty;
                if (name.Contains("win-64") && name.EndsWith(".zip"))
                {
                    downloadUrl = asset.browser_download_url?.ToString();
                    break;
                }
            }

            // If we found it, download and extract
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                var data = await _http.GetByteArrayAsync(downloadUrl, ct);
                var tempZip = Path.GetTempFileName() + ".zip";
                await File.WriteAllBytesAsync(tempZip, data, ct);

                var extractDir = Path.GetTempPath() + "aria2_extract";
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, extractDir, true);

                var exe = Directory.GetFiles(extractDir, "aria2c.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (exe != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(aria2Path)!);
                    File.Copy(exe, aria2Path, true);
                }

                // Cleanup
                File.Delete(tempZip);
                Directory.Delete(extractDir, true);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DownloadFfmpegAsync(string ffmpegPath, CancellationToken ct = default)
    {
        try
        {
            const string url = "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest";
            var json = await _http.GetStringAsync(url, ct);
            dynamic? release = JsonConvert.DeserializeObject(json);

            string? downloadUrl = null;
            foreach (var asset in release!.assets)
            {
                string name = asset.name?.ToString() ?? string.Empty;
                if (name.Contains("win64") && name.Contains("gpl") && name.EndsWith(".zip") && !name.Contains("shared"))
                {
                    downloadUrl = asset.browser_download_url?.ToString();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(downloadUrl))
            {
                var data = await _http.GetByteArrayAsync(downloadUrl, ct);
                var tempZip = Path.GetTempFileName() + ".zip";
                await File.WriteAllBytesAsync(tempZip, data, ct);

                var extractDir = Path.GetTempPath() + "ffmpeg_extract";
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, extractDir, true);

                var ffmpegDir = Path.GetDirectoryName(ffmpegPath)!;
                Directory.CreateDirectory(ffmpegDir);

                foreach (var exe in new[] { "ffmpeg.exe", "ffprobe.exe" })
                {
                    var found = Directory.GetFiles(extractDir, exe, SearchOption.AllDirectories).FirstOrDefault();
                    if (found != null)
                        File.Copy(found, Path.Combine(ffmpegDir, exe), true);
                }

                File.Delete(tempZip);
                Directory.Delete(extractDir, true);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public void OpenBrowserToRelease()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/yt-dlp/yt-dlp/releases/latest",
            UseShellExecute = true
        });
    }
}
