using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using YTDownloaderPro.Models;

namespace YTDownloaderPro.Services;

public class DownloadManager
{
    private readonly AppSettings _settings;
    private readonly DatabaseService _db;
    private readonly YtDlpService _ytDlp;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCts = new();
    private readonly SemaphoreSlim _semaphore;

    public ObservableCollection<DownloadItem> Downloads { get; } = new();

    public event Action<DownloadItem>? DownloadStarted;
    public event Action<DownloadItem>? DownloadCompleted;
    public event Action<DownloadItem>? DownloadFailed;
    public event Action<DownloadItem>? DownloadProgress;

    private int _activeCount;
    public int ActiveDownloads => _activeCount;

    public DownloadManager(AppSettings settings, DatabaseService db)
    {
        _settings = settings;
        _db = db;
        _ytDlp = new YtDlpService(settings);
        _semaphore = new SemaphoreSlim(settings.MaxConcurrentDownloads, settings.MaxConcurrentDownloads);

        // Load saved downloads
        var saved = db.GetAllDownloads();
        foreach (var item in saved)
        {
            // Reset stuck downloads
            if (item.Status == DownloadStatus.Downloading)
                item.Status = DownloadStatus.Paused;
            Downloads.Add(item);
        }
    }

    public async Task<DownloadItem> AddDownloadAsync(
        string url,
        string quality,
        string format,
        string outputPath,
        MediaType mediaType = MediaType.Video,
        bool isPlaylist = false,
        DateTime? scheduledAt = null)
    {
        var item = new DownloadItem
        {
            Url = url,
            Quality = quality,
            Format = format,
            OutputPath = outputPath,
            MediaType = mediaType,
            IsPlaylist = isPlaylist,
            ScheduledAt = scheduledAt,
            Status = scheduledAt.HasValue ? DownloadStatus.Scheduled : DownloadStatus.Queued,
            Threads = _settings.MaxThreadsPerDownload
        };

        Directory.CreateDirectory(outputPath);

        System.Windows.Application.Current.Dispatcher.Invoke(() => Downloads.Insert(0, item));
        _db.SaveDownload(item);

        if (!scheduledAt.HasValue)
            _ = StartDownloadAsync(item);

        return item;
    }

    public async Task StartDownloadAsync(DownloadItem item)
    {
        await _semaphore.WaitAsync();
        Interlocked.Increment(ref _activeCount);

        var cts = new CancellationTokenSource();
        _activeCts[item.Id] = cts;

        try
        {
            // Fetch metadata if missing
            if (string.IsNullOrEmpty(item.Title))
            {
                item.Status = DownloadStatus.Queued;
                var info = await _ytDlp.GetVideoInfoAsync(item.Url, cts.Token);
                if (info != null)
                {
                    item.Title = info.Title;
                    item.Thumbnail = info.Thumbnail;
                    item.Duration = info.DurationDisplay;
                    item.Uploader = info.Uploader;
                    if (info.IsPlaylist && info.Entries != null)
                    {
                        item.IsPlaylist = true;
                        item.PlaylistTotal = info.Entries.Count;
                    }
                }
            }

            item.Status = DownloadStatus.Downloading;
            DownloadStarted?.Invoke(item);
            _db.SaveDownload(item);

            await _ytDlp.DownloadAsync(item,
                (progress, downloaded, speed, eta) =>
                {
                    item.Progress = progress;
                    item.DownloadedBytes = downloaded;
                    item.Speed = speed;
                    item.Eta = eta;
                    _db.UpdateDownloadStatus(item.Id, item.Status, progress, downloaded, speed);
                    DownloadProgress?.Invoke(item);
                },
                cts.Token);

            if (!cts.Token.IsCancellationRequested)
            {
                item.Status = DownloadStatus.Completed;
                item.Progress = 100;
                item.CompletedAt = DateTime.Now;
                item.Speed = 0;
                DownloadCompleted?.Invoke(item);
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = DownloadStatus.Paused;
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = ex.Message;
            DownloadFailed?.Invoke(item);
        }
        finally
        {
            _db.UpdateDownloadStatus(item.Id, item.Status, item.Progress,
                item.DownloadedBytes, 0, string.Empty, item.ErrorMessage);
            _activeCts.TryRemove(item.Id, out _);
            Interlocked.Decrement(ref _activeCount);
            _semaphore.Release();
        }
    }

    public void PauseDownload(string id)
    {
        if (_activeCts.TryGetValue(id, out var cts))
        {
            cts.Cancel();
        }
        if (Downloads.FirstOrDefault(d => d.Id == id) is { } item)
        {
            if (item.Status == DownloadStatus.Downloading)
                item.Status = DownloadStatus.Paused;
        }
    }

    public void ResumeDownload(string id)
    {
        if (Downloads.FirstOrDefault(d => d.Id == id) is { } item)
        {
            if (item.Status is DownloadStatus.Paused or DownloadStatus.Failed)
            {
                item.Status = DownloadStatus.Queued;
                _ = StartDownloadAsync(item);
            }
        }
    }

    public void StopDownload(string id)
    {
        PauseDownload(id);
        if (Downloads.FirstOrDefault(d => d.Id == id) is { } item)
        {
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = "Cancelled by user";
            _db.UpdateDownloadStatus(id, DownloadStatus.Failed);
        }
    }

    public void RemoveDownload(string id, bool deleteFile = false)
    {
        StopDownload(id);
        if (Downloads.FirstOrDefault(d => d.Id == id) is { } item)
        {
            if (deleteFile && File.Exists(item.OutputPath))
                File.Delete(item.OutputPath);
            System.Windows.Application.Current.Dispatcher.Invoke(() => Downloads.Remove(item));
            _db.DeleteDownload(id);
        }
    }

    public void PauseAll()
    {
        foreach (var item in Downloads.Where(d => d.Status == DownloadStatus.Downloading).ToList())
            PauseDownload(item.Id);
    }

    public void ResumeAll()
    {
        foreach (var item in Downloads.Where(d => d.Status == DownloadStatus.Paused).ToList())
            ResumeDownload(item.Id);
    }

    public async Task AddBatchAsync(IEnumerable<string> urls, string quality, string format, string outputPath)
    {
        foreach (var url in urls)
        {
            if (!string.IsNullOrWhiteSpace(url))
                await AddDownloadAsync(url.Trim(), quality, format, outputPath);
        }
    }

    public void UpdateConcurrency(int maxConcurrent)
    {
        // Semaphore doesn't support dynamic resize, but we respect the setting on next add
        _settings.MaxConcurrentDownloads = maxConcurrent;
    }
}
