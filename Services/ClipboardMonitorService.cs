using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;

namespace YTDownloaderPro.Services;

public partial class ClipboardMonitorService : IDisposable
{
    private HwndSource? _hwndSource;
    private nint _nextClipboardViewer;
    private string _lastClipboardText = string.Empty;
    private bool _isMonitoring;

    public event Action<string>? VideoUrlDetected;
    public event Action<string>? UrlDetected;

    private static readonly string[] VideoPatterns =
    {
        @"(?:https?://)?(?:www\.)?(?:youtube\.com/watch\?v=|youtu\.be/|youtube\.com/shorts/|youtube\.com/live/)[\w-]+",
        @"(?:https?://)?(?:www\.)?vimeo\.com/\d+",
        @"(?:https?://)?(?:www\.)?dailymotion\.com/video/[\w]+",
        @"(?:https?://)?(?:www\.)?twitch\.tv/videos/\d+",
        @"(?:https?://)?(?:www\.)?tiktok\.com/@[\w.]+/video/\d+",
        @"(?:https?://)?(?:www\.)?instagram\.com/(?:p|reel|tv)/[\w-]+",
        @"(?:https?://)?(?:www\.)?twitter\.com/\w+/status/\d+",
        @"(?:https?://)?(?:www\.)?facebook\.com/(?:watch|video)/",
        @"(?:https?://)?(?:www\.)?reddit\.com/r/\w+/comments/",
    };

    private static readonly string[] PlaylistPatterns =
    {
        @"(?:https?://)?(?:www\.)?youtube\.com/playlist\?list=[\w-]+",
        @"(?:https?://)?(?:www\.)?youtube\.com/watch\?.*list=[\w-]+",
    };

    public void Start(Window window)
    {
        if (_isMonitoring) return;
        _isMonitoring = true;

        var helper = new WindowInteropHelper(window);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);
        _nextClipboardViewer = SetClipboardViewer(helper.Handle);
    }

    public void Stop()
    {
        if (!_isMonitoring) return;
        _isMonitoring = false;

        if (_hwndSource != null)
        {
            ChangeClipboardChain(_hwndSource.Handle, _nextClipboardViewer);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        const int WM_DRAWCLIPBOARD = 0x0308;
        const int WM_CHANGECBCHAIN = 0x030D;

        switch (msg)
        {
            case WM_DRAWCLIPBOARD:
                CheckClipboard();
                if (_nextClipboardViewer != IntPtr.Zero)
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                handled = false;
                break;

            case WM_CHANGECBCHAIN:
                if (wParam == _nextClipboardViewer)
                    _nextClipboardViewer = lParam;
                else if (_nextClipboardViewer != IntPtr.Zero)
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                break;
        }
        return IntPtr.Zero;
    }

    private void CheckClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text) || text == _lastClipboardText) return;
            _lastClipboardText = text;

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (IsVideoUrl(trimmed))
                {
                    VideoUrlDetected?.Invoke(trimmed);
                    return;
                }
                if (IsUrl(trimmed))
                {
                    UrlDetected?.Invoke(trimmed);
                }
            }
        }
        catch { }
    }

    public static bool IsVideoUrl(string url)
    {
        foreach (var pattern in VideoPatterns)
            if (Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))
                return true;
        return false;
    }

    public static bool IsPlaylistUrl(string url)
    {
        foreach (var pattern in PlaylistPatterns)
            if (Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))
                return true;
        return false;
    }

    public static bool IsUrl(string text)
    {
        return Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public void Dispose() => Stop();

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    private static extern nint SetClipboardViewer(nint hWndNewViewer);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChangeClipboardChain(nint hWndRemove, nint hWndNewNext);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    private static extern nint SendMessage(nint hwnd, int wMsg, nint wParam, nint lParam);
}
