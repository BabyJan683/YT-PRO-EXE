using System.Windows;
using System.Windows.Media.Imaging;
using YTDownloaderPro.Models;
using YTDownloaderPro.Services;
using YTDownloaderPro.ViewModels;
using Ookii.Dialogs.Wpf;

namespace YTDownloaderPro.Views;

public partial class AddDownloadDialog : Window
{
    private readonly MainViewModel _vm;
    private readonly AppSettings _settings;
    private VideoInfo? _fetchedInfo;
    private CancellationTokenSource? _fetchCts;

    public AddDownloadDialog(MainViewModel vm, AppSettings settings, string initialUrl = "")
    {
        InitializeComponent();
        _vm = vm;
        _settings = settings;
        TxtUrl.Text = initialUrl;
        TxtSavePath.Text = settings.DefaultDownloadPath;

        if (!string.IsNullOrEmpty(initialUrl))
        {
            // Auto-fetch if URL provided
            Loaded += async (_, _) => await FetchVideoInfoAsync(initialUrl);
        }
    }

    private async void TxtUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Auto-detect if it's a valid URL
    }

    private async void BtnFetchInfo_Click(object sender, RoutedEventArgs e)
    {
        await FetchVideoInfoAsync(TxtUrl.Text.Trim());
    }

    private async Task FetchVideoInfoAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        _fetchCts?.Cancel();
        _fetchCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        BtnDownload.IsEnabled = false;
        CardVideoInfo.Visibility = Visibility.Collapsed;
        CardFormats.Visibility = Visibility.Collapsed;

        try
        {
            var ytDlp = new YtDlpService(_settings);

            // Check if playlist
            if (ClipboardMonitorService.IsPlaylistUrl(url))
            {
                var playlist = await ytDlp.GetPlaylistInfoAsync(url, _fetchCts.Token);
                if (playlist != null)
                {
                    _fetchedInfo = playlist;
                    ShowVideoInfo(playlist, isPlaylist: true);
                    return;
                }
            }

            var info = await ytDlp.GetVideoInfoAsync(url, _fetchCts.Token);
            if (info != null)
            {
                _fetchedInfo = info;
                ShowVideoInfo(info);
            }
        }
        catch (OperationCanceledException)
        {
            // Timed out
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not fetch video info: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnDownload.IsEnabled = true;
        }
    }

    private void ShowVideoInfo(VideoInfo info, bool isPlaylist = false)
    {
        TxtVideoTitle.Text = info.Title;
        TxtDuration.Text = info.DurationDisplay;
        TxtUploader.Text = info.Uploader;

        if (isPlaylist && info.Entries != null)
        {
            TxtPlaylistInfo.Text = $"Playlist: {info.Entries.Count} videos";
            TxtPlaylistInfo.Visibility = Visibility.Visible;
            ChkPlaylist.IsChecked = true;
        }

        if (!string.IsNullOrEmpty(info.Thumbnail))
        {
            try
            {
                var bmp = new BitmapImage(new Uri(info.Thumbnail));
                ImgThumbnail.Source = bmp;
            }
            catch { }
        }

        CardVideoInfo.Visibility = Visibility.Visible;

        // Populate formats
        var formats = info.VideoFormats;
        if (formats.Count > 0)
        {
            LvFormats.ItemsSource = formats;
            CardFormats.Visibility = Visibility.Visible;
        }
    }

    private void LvFormats_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LvFormats.SelectedItem is VideoFormat fmt)
        {
            // Find matching quality in combobox or set custom
            foreach (System.Windows.Controls.ComboBoxItem item in CmbQuality.Items)
            {
                if (item.Tag?.ToString()?.Contains(fmt.Height.ToString() ?? "") == true)
                {
                    CmbQuality.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void CmbMediaType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbFormat == null) return;
        var tag = (CmbMediaType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "";

        CmbFormat.Items.Clear();
        if (tag.StartsWith("Audio"))
        {
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "MP3", Tag = "mp3" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "FLAC", Tag = "flac" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "WAV", Tag = "wav" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "M4A", Tag = "m4a" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "OGG", Tag = "vorbis" });
            CmbFormat.SelectedIndex = tag == "AudioFlac" ? 1 : tag == "AudioWav" ? 2 : 0;
        }
        else
        {
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "MP4", Tag = "mp4" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "MKV", Tag = "mkv" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "WebM", Tag = "webm" });
            CmbFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "AVI", Tag = "avi" });
            CmbFormat.SelectedIndex = 0;
        }
    }

    private void BtnPaste_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            TxtUrl.Text = Clipboard.GetText().Trim();
            _ = FetchVideoInfoAsync(TxtUrl.Text);
        }
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new VistaFolderBrowserDialog
        {
            Description = "Select download folder",
            UseDescriptionForTitle = true,
            SelectedPath = TxtSavePath.Text
        };
        if (dlg.ShowDialog(this) == true)
            TxtSavePath.Text = dlg.SelectedPath;
    }

    private void ChkSchedule_Changed(object sender, RoutedEventArgs e)
    {
        if (PnlSchedule != null)
            PnlSchedule.Visibility = ChkSchedule.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtUrl.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a URL.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var quality = ((System.Windows.Controls.ComboBoxItem)CmbQuality.SelectedItem)?.Tag?.ToString()
            ?? "bestvideo+bestaudio/best";
        var format = ((System.Windows.Controls.ComboBoxItem)CmbFormat.SelectedItem)?.Tag?.ToString()
            ?? "mp4";
        var mediaTypeTag = ((System.Windows.Controls.ComboBoxItem)CmbMediaType.SelectedItem)?.Tag?.ToString()
            ?? "Video";
        var mediaType = mediaTypeTag.StartsWith("Audio") ? MediaType.Audio : MediaType.Video;
        var savePath = TxtSavePath.Text;
        var isPlaylist = ChkPlaylist.IsChecked == true;

        DateTime? scheduledAt = null;
        if (ChkSchedule.IsChecked == true && DpSchedule.SelectedDate.HasValue)
        {
            var date = DpSchedule.SelectedDate.Value;
            if (TimeSpan.TryParse(TxtScheduleTime.Text, out var time))
                scheduledAt = date.Add(time);
        }

        if (mediaType == MediaType.Audio)
            quality = "bestaudio/best";

        await _vm.AddDownloadFromUrl(url, quality, format, savePath, mediaType, isPlaylist, scheduledAt);

        if (_fetchedInfo != null)
            _vm.Downloads[0].Title = _fetchedInfo.Title;

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _fetchCts?.Cancel();
        DialogResult = false;
        Close();
    }
}
