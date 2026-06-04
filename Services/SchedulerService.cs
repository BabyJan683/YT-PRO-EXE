using YTDownloaderPro.Models;

namespace YTDownloaderPro.Services;

public class SchedulerService : IDisposable
{
    private readonly DownloadManager _downloadManager;
    private readonly AppSettings _settings;
    private System.Threading.Timer? _timer;
    private bool _isRunning;

    public event Action? SchedulerStarted;
    public event Action? SchedulerStopped;

    public bool IsRunning => _isRunning;

    public SchedulerService(DownloadManager downloadManager, AppSettings settings)
    {
        _downloadManager = downloadManager;
        _settings = settings;
    }

    public void Start()
    {
        _timer = new System.Threading.Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void Tick(object? state)
    {
        var now = DateTime.Now;
        CheckScheduledDownloads(now);

        if (_settings.EnableScheduler)
        {
            var startTime = ParseTime(_settings.ScheduledStartTime);
            var stopTime = ParseTime(_settings.ScheduledStopTime);

            if (IsInWindow(now.TimeOfDay, startTime, stopTime))
            {
                if (!_isRunning)
                {
                    _isRunning = true;
                    _downloadManager.ResumeAll();
                    SchedulerStarted?.Invoke();
                }
            }
            else
            {
                if (_isRunning)
                {
                    _isRunning = false;
                    _downloadManager.PauseAll();
                    SchedulerStopped?.Invoke();
                }
            }
        }
    }

    private void CheckScheduledDownloads(DateTime now)
    {
        foreach (var item in _downloadManager.Downloads.ToList())
        {
            if (item.Status == DownloadStatus.Scheduled
                && item.ScheduledAt.HasValue
                && now >= item.ScheduledAt.Value)
            {
                item.Status = DownloadStatus.Queued;
                _ = _downloadManager.StartDownloadAsync(item);
            }
        }
    }

    private static TimeSpan ParseTime(string timeStr)
    {
        if (TimeSpan.TryParse(timeStr, out var ts)) return ts;
        return TimeSpan.Zero;
    }

    private static bool IsInWindow(TimeSpan current, TimeSpan start, TimeSpan end)
    {
        if (start <= end)
            return current >= start && current <= end;
        else
            return current >= start || current <= end;
    }

    public void ScheduleDownload(DownloadItem item, DateTime scheduledAt)
    {
        item.ScheduledAt = scheduledAt;
        item.Status = DownloadStatus.Scheduled;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
