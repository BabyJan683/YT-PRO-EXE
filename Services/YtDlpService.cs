using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using YTDownloaderPro.Models;

namespace YTDownloaderPro.Services;

public partial class YtDlpService
{
    private readonly AppSettings _settings;

    public YtDlpService(AppSettings settings)
    {
        _settings = settings;
    }

    private string GetYtDlpPath()
    {
        var paths = new[]
        {
            _settings.YtDlpPath,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "yt-dlp.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe"),
            "yt-dlp.exe"
        };

        foreach (var path in paths)
            if (File.Exists(path)) return path;

        return "yt-dlp";
    }

    public async Task<VideoInfo?> GetVideoInfoAsync(string url, CancellationToken ct = default)
    {
        var args = BuildArgs(url, new[] { "--dump-json", "--no-playlist" });
        var output = await RunProcessAsync(GetYtDlpPath(), args, ct);
        if (string.IsNullOrEmpty(output)) return null;

        try
        {
            return JsonConvert.DeserializeObject<VideoInfo>(output);
        }
        catch
        {
            return null;
        }
    }

    public async Task<VideoInfo?> GetPlaylistInfoAsync(string url, CancellationToken ct = default)
    {
        var args = BuildArgs(url, new[] { "--dump-json", "--flat-playlist" });
        var output = await RunProcessAsync(GetYtDlpPath(), args, ct);
        if (string.IsNullOrEmpty(output)) return null;

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var entries = new List<VideoInfo>();
        foreach (var line in lines)
        {
            try
            {
                var info = JsonConvert.DeserializeObject<VideoInfo>(line);
                if (info != null) entries.Add(info);
            }
            catch { }
        }

        return entries.Count > 0
            ? new VideoInfo { Title = "Playlist", Entries = entries }
            : null;
    }

    public async Task DownloadAsync(
        DownloadItem item,
        Action<double, long, double, string>? onProgress,
        CancellationToken ct)
    {
        var outputTemplate = Path.Combine(item.OutputPath, "%(title)s.%(ext)s");
        var args = new List<string>
        {
            $"\"{item.Url}\"",
            $"-o \"{outputTemplate}\"",
            "--newline",
            "--progress",
        };

        if (item.MediaType == MediaType.Audio)
        {
            args.Add("-x");
            args.Add($"--audio-format {item.Format ?? "mp3"}");
            args.Add("--audio-quality 0");
        }
        else
        {
            var quality = item.Quality ?? "bestvideo+bestaudio/best";
            args.Add($"-f \"{quality}\"");
            args.Add($"--merge-output-format {item.Format ?? "mp4"}");
        }

        // Embed thumbnail & metadata
        args.Add("--embed-thumbnail");
        args.Add("--add-metadata");

        // Use aria2 for faster downloads
        var aria2Path = _settings.Aria2Path;
        if (!File.Exists(aria2Path))
            aria2Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "aria2c.exe");

        if (File.Exists(aria2Path))
        {
            args.Add($"--downloader aria2c");
            args.Add($"--downloader-args \"aria2c:-x {item.Threads} -s {item.Threads} -k 1M --file-allocation=none\"");
        }

        // FFmpeg path
        var ffmpegDir = Path.GetDirectoryName(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ffmpeg.exe"));
        if (!string.IsNullOrEmpty(ffmpegDir))
            args.Add($"--ffmpeg-location \"{ffmpegDir}\"");

        // Speed limit
        if (_settings.LimitSpeed && _settings.SpeedLimitKBps > 0)
            args.Add($"--limit-rate {_settings.SpeedLimitKBps}K");

        // Proxy
        if (_settings.UseProxy && !string.IsNullOrEmpty(_settings.ProxyAddress))
            args.Add($"--proxy http://{_settings.ProxyAddress}:{_settings.ProxyPort}");

        // Subtitles
        if (_settings.WriteSubtitles)
        {
            args.Add("--write-subs");
            args.Add($"--sub-langs \"{_settings.SubtitleLanguages}\"");
        }

        // Cookies
        if (_settings.UseCookies && !string.IsNullOrEmpty(_settings.CookiesFile))
            args.Add($"--cookies \"{_settings.CookiesFile}\"");
        else if (_settings.UseCookies && !string.IsNullOrEmpty(_settings.CookiesFromBrowser))
            args.Add($"--cookies-from-browser {_settings.CookiesFromBrowser}");

        // Playlist
        if (item.IsPlaylist)
        {
            args.Add("--yes-playlist");
            if (item.PlaylistIndex > 0)
                args.Add($"--playlist-start {item.PlaylistIndex}");
        }
        else
        {
            args.Add("--no-playlist");
        }

        // Resume support
        args.Add("--continue");
        args.Add($"--retries {_settings.MaxRetriesOnError}");

        var fullArgs = string.Join(" ", args);
        var detectedFile = await RunDownloadProcessAsync(GetYtDlpPath(), fullArgs, onProgress, ct);

        // Populate item.FileName so Open File/Folder works correctly
        if (!string.IsNullOrEmpty(detectedFile) && File.Exists(detectedFile))
        {
            item.FileName = Path.GetFileName(detectedFile);
            item.OutputPath = Path.GetDirectoryName(detectedFile) ?? item.OutputPath;
            item.FileSize = new FileInfo(detectedFile).Length;
        }
        else
        {
            // Fallback: find the most recently modified file in the output folder
            if (Directory.Exists(item.OutputPath))
            {
                var recent = new DirectoryInfo(item.OutputPath)
                    .GetFiles()
                    .Where(f => (DateTime.Now - f.LastWriteTime).TotalMinutes < 10)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                if (recent != null)
                {
                    item.FileName = recent.Name;
                    item.FileSize = recent.Length;
                }
            }
        }
    }

    private async Task<string?> RunDownloadProcessAsync(
        string exe,
        string args,
        Action<double, long, double, string>? onProgress,
        CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        process.Start();

        // CRITICAL: drain stderr in parallel — if not read, the pipe buffer fills,
        // yt-dlp/aria2 blocks, and the download hangs or fails incorrectly.
        var stderrBuilder = new StringBuilder();
        var stderrTask = Task.Run(async () =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var l = await process.StandardError.ReadLineAsync();
                if (l != null) stderrBuilder.AppendLine(l);
            }
        });

        var progressPattern = ProgressRegex();
        string? detectedFile = null;

        while (!process.StandardOutput.EndOfStream)
        {
            if (ct.IsCancellationRequested)
            {
                try { process.Kill(true); } catch { }
                break;
            }

            string? line;
            try { line = await process.StandardOutput.ReadLineAsync(ct); }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                throw;
            }
            if (line == null) continue;

            // Parse progress: [download]  45.6% of  123.45MiB at  2.34MiB/s ETA 00:30
            var match = progressPattern.Match(line);
            if (match.Success)
            {
                double.TryParse(match.Groups["pct"].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var pct);
                var speedStr = match.Groups["speed"].Value;
                var eta = match.Groups["eta"].Value;
                var speed = ParseSpeed(speedStr);
                onProgress?.Invoke(pct, 0, speed, eta);
            }

            // Capture the output file path from yt-dlp log lines
            if (detectedFile == null)
                detectedFile = TryParseOutputFile(line);
        }

        await process.WaitForExitAsync(ct);
        await stderrTask;

        // Only treat non-zero exit as an error when not cancelled
        if (!ct.IsCancellationRequested && process.ExitCode != 0)
        {
            var errText = stderrBuilder.ToString().Trim();
            if (string.IsNullOrEmpty(errText)) errText = $"yt-dlp exited with code {process.ExitCode}";
            throw new Exception(errText);
        }

        return detectedFile;
    }

    private static string? TryParseOutputFile(string line)
    {
        // Match common yt-dlp output lines that reveal the destination file
        var patterns = new[]
        {
            @"\[download\] Destination: (.+)",
            @"\[Merger\] Merging formats into ""(.+)""",
            @"\[ExtractAudio\] Destination: (.+)",
            @"\[VideoConvertor\] Converting video .+ to: (.+)",
            @"\[ffmpeg\] Destination: (.+)",
        };
        foreach (var p in patterns)
        {
            var m = Regex.Match(line, p);
            if (m.Success) return m.Groups[1].Value.Trim();
        }
        return null;
    }

    private static double ParseSpeed(string speedStr)
    {
        if (string.IsNullOrEmpty(speedStr)) return 0;
        var match = Regex.Match(speedStr, @"([\d.]+)\s*(B|KB|MB|GB|KiB|MiB|GiB)/s");
        if (!match.Success) return 0;
        double.TryParse(match.Groups[1].Value, out var val);
        return match.Groups[2].Value.ToUpper() switch
        {
            "KB" or "KIB" => val * 1024,
            "MB" or "MIB" => val * 1024 * 1024,
            "GB" or "GIB" => val * 1024 * 1024 * 1024,
            _ => val
        };
    }

    private List<string> BuildArgs(string url, IEnumerable<string> extra)
    {
        var args = new List<string> { $"\"{url}\"" };
        args.AddRange(extra);
        if (_settings.UseCookies && !string.IsNullOrEmpty(_settings.CookiesFile))
            args.Add($"--cookies \"{_settings.CookiesFile}\"");
        if (_settings.UseProxy && !string.IsNullOrEmpty(_settings.ProxyAddress))
            args.Add($"--proxy http://{_settings.ProxyAddress}:{_settings.ProxyPort}");
        return args;
    }

    private static async Task<string> RunProcessAsync(string exe, IEnumerable<string> args, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        var sb = new StringBuilder();
        process.Start();
        while (!process.StandardOutput.EndOfStream)
        {
            if (ct.IsCancellationRequested) { process.Kill(true); break; }
            var line = await process.StandardOutput.ReadLineAsync(ct);
            if (line != null) sb.AppendLine(line);
        }
        await process.WaitForExitAsync(ct);
        return sb.ToString();
    }

    public async Task UpdateYtDlpAsync(CancellationToken ct = default)
    {
        await RunProcessAsync(GetYtDlpPath(), new[] { "--update" }, ct);
    }

    [GeneratedRegex(@"\[download\]\s+(?<pct>[\d.]+)%\s+of\s+~?[\d.]+\S+\s+at\s+(?<speed>[\d.]+\s*\S+/s)\s+ETA\s+(?<eta>\S+)", RegexOptions.Compiled)]
    private static partial Regex ProgressRegex();

    [GeneratedRegex(@"([\d.]+)\s*(B|KB|MB|GB|KiB|MiB|GiB)", RegexOptions.Compiled)]
    private static partial Regex SizeRegex();
}
