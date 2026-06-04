using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YTDownloaderPro.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
    Scheduled,
    Merging,
    Converting
}

public enum MediaType
{
    Video,
    Audio,
    Playlist
}

public class DownloadItem : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private string _url = string.Empty;
    private string _title = string.Empty;
    private string _thumbnail = string.Empty;
    private string _outputPath = string.Empty;
    private string _fileName = string.Empty;
    private string _quality = string.Empty;
    private string _format = string.Empty;
    private long _totalBytes;
    private long _downloadedBytes;
    private double _speed;
    private string _eta = string.Empty;
    private DownloadStatus _status = DownloadStatus.Queued;
    private MediaType _mediaType = MediaType.Video;
    private double _progress;
    private string _errorMessage = string.Empty;
    private DateTime _createdAt = DateTime.Now;
    private DateTime? _completedAt;
    private DateTime? _scheduledAt;
    private int _threads = 8;
    private bool _isPlaylist;
    private int _playlistIndex;
    private int _playlistTotal;
    private string _uploader = string.Empty;
    private string _duration = string.Empty;
    private long _fileSize;

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Thumbnail
    {
        get => _thumbnail;
        set { _thumbnail = value; OnPropertyChanged(); }
    }

    public string OutputPath
    {
        get => _outputPath;
        set { _outputPath = value; OnPropertyChanged(); }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public string Quality
    {
        get => _quality;
        set { _quality = value; OnPropertyChanged(); }
    }

    public string Format
    {
        get => _format;
        set { _format = value; OnPropertyChanged(); }
    }

    public long TotalBytes
    {
        get => _totalBytes;
        set
        {
            _totalBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalSize));
        }
    }

    public long DownloadedBytes
    {
        get => _downloadedBytes;
        set
        {
            _downloadedBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DownloadedSize));
        }
    }

    public double Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpeedDisplay));
        }
    }

    public string Eta
    {
        get => _eta;
        set { _eta = value; OnPropertyChanged(); }
    }

    public DownloadStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanResume));
            OnPropertyChanged(nameof(CanStop));
        }
    }

    public MediaType MediaType
    {
        get => _mediaType;
        set { _mediaType = value; OnPropertyChanged(); }
    }

    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set { _createdAt = value; OnPropertyChanged(); }
    }

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set { _completedAt = value; OnPropertyChanged(); }
    }

    public DateTime? ScheduledAt
    {
        get => _scheduledAt;
        set { _scheduledAt = value; OnPropertyChanged(); }
    }

    public int Threads
    {
        get => _threads;
        set { _threads = value; OnPropertyChanged(); }
    }

    public bool IsPlaylist
    {
        get => _isPlaylist;
        set { _isPlaylist = value; OnPropertyChanged(); }
    }

    public int PlaylistIndex
    {
        get => _playlistIndex;
        set { _playlistIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaylistProgress)); }
    }

    public int PlaylistTotal
    {
        get => _playlistTotal;
        set { _playlistTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaylistProgress)); }
    }

    public string Uploader
    {
        get => _uploader;
        set { _uploader = value; OnPropertyChanged(); }
    }

    public string Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); }
    }

    public long FileSize
    {
        get => _fileSize;
        set { _fileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeDisplay)); }
    }

    // Computed properties
    public string TotalSize => FormatBytes(TotalBytes);
    public string DownloadedSize => FormatBytes(DownloadedBytes);
    public string SpeedDisplay => Speed > 0 ? $"{FormatBytes((long)Speed)}/s" : string.Empty;
    public string FileSizeDisplay => FormatBytes(FileSize);
    public bool IsActive => Status == DownloadStatus.Downloading || Status == DownloadStatus.Merging;
    public bool CanPause => Status == DownloadStatus.Downloading;
    public bool CanResume => Status == DownloadStatus.Paused || Status == DownloadStatus.Failed;
    public bool CanStop => Status == DownloadStatus.Downloading || Status == DownloadStatus.Paused || Status == DownloadStatus.Queued;

    public string PlaylistProgress => IsPlaylist && PlaylistTotal > 0
        ? $"{PlaylistIndex}/{PlaylistTotal}"
        : string.Empty;

    public string StatusDisplay => Status switch
    {
        DownloadStatus.Queued => "Queued",
        DownloadStatus.Downloading => $"{Progress:F1}%",
        DownloadStatus.Paused => "Paused",
        DownloadStatus.Completed => "Completed",
        DownloadStatus.Failed => "Failed",
        DownloadStatus.Scheduled => $"Scheduled: {ScheduledAt:HH:mm}",
        DownloadStatus.Merging => "Merging...",
        DownloadStatus.Converting => "Converting...",
        _ => Status.ToString()
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
