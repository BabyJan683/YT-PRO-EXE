using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using YTDownloaderPro.Models;
using YTDownloaderPro.Services;

namespace YTDownloaderPro.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DownloadManager _downloadManager;
    private readonly SettingsService _settingsService;
    private readonly ClipboardMonitorService _clipboardMonitor;
    private readonly UpdateService _updateService;
    private readonly SchedulerService _scheduler;
    private readonly BrowserExtensionServer _browserServer;

    private string _urlInput = string.Empty;
    private string _statusText = "Ready";
    private string _speedText = string.Empty;
    private bool _isBusy;
    private DownloadItem? _selectedDownload;
    private string _searchFilter = string.Empty;
    private string _currentCategory = "All";
    private double _totalSpeed;
    private int _activeCount;
    private string _theme = "Dark";

    public AppSettings Settings => _settingsService.Settings;

    public ObservableCollection<DownloadItem> Downloads => _downloadManager.Downloads;

    public ObservableCollection<DownloadItem> FilteredDownloads { get; } = new();

    public string UrlInput
    {
        get => _urlInput;
        set { _urlInput = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string SpeedText
    {
        get => _speedText;
        set { _speedText = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public DownloadItem? SelectedDownload
    {
        get => _selectedDownload;
        set { _selectedDownload = value; OnPropertyChanged(); }
    }

    public string SearchFilter
    {
        get => _searchFilter;
        set { _searchFilter = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public string CurrentCategory
    {
        get => _currentCategory;
        set { _currentCategory = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public double TotalSpeed
    {
        get => _totalSpeed;
        set { _totalSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalSpeedDisplay)); }
    }

    public int ActiveCount
    {
        get => _activeCount;
        set { _activeCount = value; OnPropertyChanged(); }
    }

    public string TotalSpeedDisplay
    {
        get
        {
            if (TotalSpeed <= 0) return string.Empty;
            string[] units = { "B/s", "KB/s", "MB/s", "GB/s" };
            int i = 0; double s = TotalSpeed;
            while (s >= 1024 && i < units.Length - 1) { s /= 1024; i++; }
            return $"{s:F1} {units[i]}";
        }
    }

    public string Theme
    {
        get => _theme;
        set { _theme = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand AddDownloadCommand { get; }
    public ICommand PasteAndDownloadCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand PauseAllCommand { get; }
    public ICommand ResumeAllCommand { get; }
    public ICommand ClearCompletedCommand { get; }
    public ICommand OpenFileCommand { get; }

    public MainViewModel(DownloadManager downloadManager, SettingsService settingsService,
        ClipboardMonitorService clipboardMonitor, UpdateService updateService,
        SchedulerService scheduler, BrowserExtensionServer browserServer)
    {
        _downloadManager = downloadManager;
        _settingsService = settingsService;
        _clipboardMonitor = clipboardMonitor;
        _updateService = updateService;
        _scheduler = scheduler;
        _browserServer = browserServer;
        _theme = settingsService.Settings.Theme;

        AddDownloadCommand = new RelayCommand(OnAddDownload);
        PasteAndDownloadCommand = new RelayCommand(OnPasteAndDownload);
        PauseCommand = new RelayCommand<DownloadItem>(item => item != null && _downloadManager != null
            ? _downloadManager.PauseDownload(item.Id) : default);
        ResumeCommand = new RelayCommand<DownloadItem>(item => item != null
            ? _downloadManager.ResumeDownload(item.Id) : default);
        StopCommand = new RelayCommand<DownloadItem>(item => item != null
            ? _downloadManager.StopDownload(item.Id) : default);
        RemoveCommand = new RelayCommand<DownloadItem>(item => item != null
            ? _downloadManager.RemoveDownload(item.Id) : default);
        OpenFolderCommand = new RelayCommand<DownloadItem>(OpenFolder);
        PauseAllCommand = new RelayCommand(_downloadManager.PauseAll);
        ResumeAllCommand = new RelayCommand(_downloadManager.ResumeAll);
        ClearCompletedCommand = new RelayCommand(ClearCompleted);
        OpenFileCommand = new RelayCommand<DownloadItem>(OpenFile);

        // Wire events
        _downloadManager.DownloadCompleted += OnDownloadCompleted;
        _downloadManager.DownloadFailed += OnDownloadFailed;
        _downloadManager.DownloadProgress += OnDownloadProgressUpdate;

        _clipboardMonitor.VideoUrlDetected += url =>
        {
            if (Settings.MonitorClipboard && Settings.ShowVideoPopup)
                Application.Current.Dispatcher.Invoke(() => PromptVideoDownload(url));
        };

        _browserServer.VideoDetected += (url, title) =>
            Application.Current.Dispatcher.Invoke(() => PromptVideoDownload(url, title));

        // Load initial filter
        ApplyFilter();

        // Subscribe to collection changes
        Downloads.CollectionChanged += (_, _) => ApplyFilter();

        // Speed update timer
        var speedTimer = new System.Threading.Timer(_ =>
        {
            var speed = Downloads.Where(d => d.IsActive).Sum(d => d.Speed);
            var count = Downloads.Count(d => d.IsActive);
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TotalSpeed = speed;
                ActiveCount = count;
            });
        }, null, 0, 1000);
    }

    private void OnAddDownload()
    {
        if (string.IsNullOrWhiteSpace(UrlInput)) return;
        // Open the add dialog
        Application.Current.MainWindow?.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow is Views.MainWindow mw)
                mw.ShowAddDownloadDialog(UrlInput);
        });
    }

    private void OnPasteAndDownload()
    {
        if (System.Windows.Clipboard.ContainsText())
            UrlInput = System.Windows.Clipboard.GetText().Trim();
    }

    public async Task AddDownloadFromUrl(string url, string quality, string format,
        string outputPath, MediaType mediaType = MediaType.Video, bool isPlaylist = false,
        DateTime? scheduledAt = null)
    {
        await _downloadManager.AddDownloadAsync(url, quality, format, outputPath,
            mediaType, isPlaylist, scheduledAt);
        UrlInput = string.Empty;
    }

    private void OnDownloadCompleted(DownloadItem item)
    {
        StatusText = $"Completed: {item.Title}";
        ApplyFilter();
    }

    private void OnDownloadFailed(DownloadItem item)
    {
        StatusText = $"Failed: {item.Title} - {item.ErrorMessage}";
    }

    private void OnDownloadProgressUpdate(DownloadItem item)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            FilteredDownloads.Clear();
            var items = Downloads.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchFilter))
                items = items.Where(d =>
                    d.Title.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    d.Url.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

            items = CurrentCategory switch
            {
                "Downloading" => items.Where(d => d.Status == DownloadStatus.Downloading),
                "Completed" => items.Where(d => d.Status == DownloadStatus.Completed),
                "Paused" => items.Where(d => d.Status == DownloadStatus.Paused),
                "Failed" => items.Where(d => d.Status == DownloadStatus.Failed),
                "Scheduled" => items.Where(d => d.Status == DownloadStatus.Scheduled),
                "Video" => items.Where(d => d.MediaType == MediaType.Video),
                "Audio" => items.Where(d => d.MediaType == MediaType.Audio),
                _ => items
            };

            foreach (var item in items)
                FilteredDownloads.Add(item);
        });
    }

    private void ClearCompleted()
    {
        foreach (var item in Downloads.Where(d => d.Status == DownloadStatus.Completed).ToList())
            _downloadManager.RemoveDownload(item.Id);
    }

    private void OpenFolder(DownloadItem? item)
    {
        if (item == null) return;

        // Try to select the specific file in Explorer if we know the filename
        if (!string.IsNullOrEmpty(item.FileName))
        {
            var filePath = Path.Combine(item.OutputPath, item.FileName);
            if (File.Exists(filePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                return;
            }
        }

        // Fallback: open the output directory directly
        var dir = Directory.Exists(item.OutputPath)
            ? item.OutputPath
            : Path.GetDirectoryName(item.OutputPath);

        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            System.Diagnostics.Process.Start("explorer.exe", $"\"{dir}\"");
        else
            MessageBox.Show($"Folder not found:\n{item.OutputPath}",
                "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OpenFile(DownloadItem? item)
    {
        if (item == null) return;

        // Try full path combinations
        var candidates = new List<string>();

        if (!string.IsNullOrEmpty(item.FileName))
            candidates.Add(Path.Combine(item.OutputPath, item.FileName));

        // Also try the OutputPath directly in case it was set to the file path
        if (!string.IsNullOrEmpty(item.OutputPath) && Path.HasExtension(item.OutputPath))
            candidates.Add(item.OutputPath);

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = candidate,
                    UseShellExecute = true
                });
                return;
            }
        }

        // File not found — open the folder instead
        OpenFolder(item);
    }

    private void PromptVideoDownload(string url, string title = "")
    {
        // Show the video popup
        if (Application.Current.MainWindow is Views.MainWindow mw)
            mw.ShowVideoDetectedPopup(url, title);
    }

    public void ToggleTheme()
    {
        Theme = Theme == "Dark" ? "Light" : "Dark";
        Settings.Theme = Theme;
        _settingsService.Save();
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(Theme == "Dark"
            ? MaterialDesignThemes.Wpf.BaseTheme.Dark
            : MaterialDesignThemes.Wpf.BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
