using Newtonsoft.Json;

namespace YTDownloaderPro.Models;

public class VideoFormat
{
    [JsonProperty("format_id")]
    public string FormatId { get; set; } = string.Empty;

    [JsonProperty("ext")]
    public string Extension { get; set; } = string.Empty;

    [JsonProperty("width")]
    public int? Width { get; set; }

    [JsonProperty("height")]
    public int? Height { get; set; }

    [JsonProperty("fps")]
    public double? Fps { get; set; }

    [JsonProperty("vcodec")]
    public string VideoCodec { get; set; } = string.Empty;

    [JsonProperty("acodec")]
    public string AudioCodec { get; set; } = string.Empty;

    [JsonProperty("filesize")]
    public long? FileSize { get; set; }

    [JsonProperty("filesize_approx")]
    public long? FileSizeApprox { get; set; }

    [JsonProperty("tbr")]
    public double? TotalBitrate { get; set; }

    [JsonProperty("vbr")]
    public double? VideoBitrate { get; set; }

    [JsonProperty("abr")]
    public double? AudioBitrate { get; set; }

    [JsonProperty("format_note")]
    public string FormatNote { get; set; } = string.Empty;

    [JsonProperty("protocol")]
    public string Protocol { get; set; } = string.Empty;

    public bool HasVideo => VideoCodec != "none" && !string.IsNullOrEmpty(VideoCodec);
    public bool HasAudio => AudioCodec != "none" && !string.IsNullOrEmpty(AudioCodec);

    public string Resolution => Width.HasValue && Height.HasValue
        ? $"{Width}x{Height}"
        : HasAudio && !HasVideo ? "Audio only" : "Unknown";

    public string QualityLabel => Height.HasValue
        ? $"{Height}p{(Fps.HasValue && Fps > 30 ? Fps.Value.ToString("F0") : "")}"
        : FormatNote;

    public long EstimatedSize => FileSize ?? FileSizeApprox ?? 0;

    public string FileSizeDisplay
    {
        get
        {
            long size = EstimatedSize;
            if (size <= 0) return "~?";
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double s = size;
            while (s >= 1024 && order < sizes.Length - 1) { order++; s /= 1024; }
            return $"~{s:F1} {sizes[order]}";
        }
    }

    public string DisplayName => $"{QualityLabel} ({Extension}) {FileSizeDisplay}";
}

public class VideoInfo
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("uploader")]
    public string Uploader { get; set; } = string.Empty;

    [JsonProperty("upload_date")]
    public string UploadDate { get; set; } = string.Empty;

    [JsonProperty("duration")]
    public double? Duration { get; set; }

    [JsonProperty("view_count")]
    public long? ViewCount { get; set; }

    [JsonProperty("like_count")]
    public long? LikeCount { get; set; }

    [JsonProperty("thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;

    [JsonProperty("webpage_url")]
    public string WebpageUrl { get; set; } = string.Empty;

    [JsonProperty("extractor")]
    public string Extractor { get; set; } = string.Empty;

    [JsonProperty("formats")]
    public List<VideoFormat> Formats { get; set; } = new();

    [JsonProperty("is_live")]
    public bool IsLive { get; set; }

    [JsonProperty("playlist")]
    public string? Playlist { get; set; }

    [JsonProperty("playlist_index")]
    public int? PlaylistIndex { get; set; }

    [JsonProperty("playlist_count")]
    public int? PlaylistCount { get; set; }

    [JsonProperty("entries")]
    public List<VideoInfo>? Entries { get; set; }

    public string DurationDisplay
    {
        get
        {
            if (!Duration.HasValue) return "Live";
            var ts = TimeSpan.FromSeconds(Duration.Value);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }

    public bool IsPlaylist => Entries != null && Entries.Count > 0;

    public List<VideoFormat> VideoFormats => Formats
        .Where(f => f.HasVideo && f.Height.HasValue)
        .OrderByDescending(f => f.Height)
        .ThenByDescending(f => f.Fps)
        .ToList();

    public List<VideoFormat> AudioFormats => Formats
        .Where(f => f.HasAudio && !f.HasVideo)
        .OrderByDescending(f => f.AudioBitrate)
        .ToList();

    public List<VideoFormat> CombinedFormats => Formats
        .Where(f => f.HasVideo && f.HasAudio)
        .OrderByDescending(f => f.Height)
        .ToList();
}
