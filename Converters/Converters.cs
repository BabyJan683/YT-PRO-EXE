using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using YTDownloaderPro.Models;

namespace YTDownloaderPro.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (DownloadStatus)value switch
        {
            DownloadStatus.Downloading => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
            DownloadStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),      // Green
            DownloadStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 67, 54)),         // Red
            DownloadStatus.Paused => new SolidColorBrush(Color.FromRgb(255, 152, 0)),         // Orange
            DownloadStatus.Scheduled => new SolidColorBrush(Color.FromRgb(156, 39, 176)),     // Purple
            DownloadStatus.Merging => new SolidColorBrush(Color.FromRgb(0, 188, 212)),        // Cyan
            DownloadStatus.Converting => new SolidColorBrush(Color.FromRgb(63, 81, 181)),     // Indigo
            _ => new SolidColorBrush(Color.FromRgb(120, 120, 120))                            // Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (DownloadStatus)value switch
        {
            DownloadStatus.Downloading => "Download",
            DownloadStatus.Completed => "CheckCircle",
            DownloadStatus.Failed => "AlertCircle",
            DownloadStatus.Paused => "PauseCircle",
            DownloadStatus.Queued => "ClockOutline",
            DownloadStatus.Scheduled => "CalendarClock",
            DownloadStatus.Merging => "Merge",
            DownloadStatus.Converting => "Cog",
            _ => "HelpCircle"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProgressToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return 0.0;
        if (values[0] is double progress && values[1] is double totalWidth)
            return totalWidth * (progress / 100.0);
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProgressToWidthSingleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Used as a relative width, actual binding uses ElementName
        return value is double d ? d : 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value is bool bv && bv;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

public class BytesToSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:F2} {sizes[order]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int i && i == 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class MediaTypeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (MediaType)value switch
        {
            MediaType.Audio => "MusicNote",
            MediaType.Playlist => "PlaylistPlay",
            _ => "Video"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProgressToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d && d > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
