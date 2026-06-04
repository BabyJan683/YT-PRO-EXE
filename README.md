# YT Downloader Pro v2.0

A professional, free, open-source video downloader with an IDM-style interface. Powered by **yt-dlp**, **aria2**, and **FFmpeg**.

## Features

| Feature | Status |
|---------|--------|
| Multi-thread downloading (aria2, up to 32 threads) | ✅ |
| Resume support | ✅ |
| Download scheduling | ✅ |
| Browser extension (Chrome/Edge/Firefox) | ✅ |
| Video detection popup | ✅ |
| Playlist downloading | ✅ |
| Quality selector (4K/1440p/1080p/720p/480p/360p) | ✅ |
| System tray mode | ✅ |
| Start with Windows | ✅ |
| Download history (SQLite) | ✅ |
| Clipboard monitoring | ✅ |
| Automatic updates (yt-dlp) | ✅ |
| Dark and light themes | ✅ |
| Batch downloads | ✅ |
| Silent installation (Inno Setup) | ✅ |
| Portable mode | ✅ |
| Windows 8.1, 10, 11 support | ✅ |
| IDM-style interface | ✅ |

## Bug Fixed (v2.0)

**Error: `InvalidOperationException: SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize`**

This was caused by setting window position/splitter values before the window finished loading. Fixed by:
- Moving all window position restoration to the `Window_Loaded` event (after the window is rendered)
- Clamping all position/size values to valid screen bounds using `SystemParameters.WorkArea`
- Using `GridSplitter` with safe min-width constraints instead of `SplitContainer`

## Requirements

- Windows 8.1 / 10 / 11 (64-bit)
- .NET 8 Runtime ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- yt-dlp.exe, aria2c.exe, ffmpeg.exe (auto-downloaded on first run)

## Quick Start

### Build from source

```powershell
# 1. Clone and enter directory
cd YTDownloaderPro

# 2. Download dependencies + build
.\build.ps1 -DownloadDependencies

# 3. Run
.\bin\Release\net8.0-windows\win-x64\publish\YTDownloaderPro.exe
```

### Create installer

```powershell
# Requires Inno Setup 6 installed
.\build.ps1 -DownloadDependencies -CreateInstaller
```

### Silent install

```cmd
YTDownloaderPro_v2.0.0_Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

### Portable mode

Place a file named `portable.txt` next to `YTDownloaderPro.exe` — all data is stored in the same folder.

## Browser Extension

1. Open Chrome/Edge → Extensions → Enable Developer Mode
2. Click "Load unpacked" → select the `Browser/` folder
3. The extension detects videos and sends them to the app

## Supported Sites

1000+ sites via yt-dlp including:
YouTube, YouTube Music, Vimeo, Dailymotion, Twitch, TikTok, Instagram, Twitter/X,
Facebook, Reddit, SoundCloud, Bandcamp, Bilibili, NicoNico, and more.

## Open-Source Components

| Component | License | Purpose |
|-----------|---------|---------|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Unlicense | Video extraction |
| [aria2](https://github.com/aria2/aria2) | GPL-2.0 | Multi-threaded downloading |
| [FFmpeg](https://ffmpeg.org) | LGPL/GPL | Merging & conversion |
| [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | MIT | UI framework |
| [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite) | MIT | Local database |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | MIT | JSON parsing |
| [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) | MIT | System tray |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MIT | MVVM framework |
| [Ookii.Dialogs.Wpf](https://github.com/ookii-dialogs/ookii-dialogs-wpf) | BSD-3 | File dialogs |

## Architecture

```
YTDownloaderPro/
├── Models/          # DownloadItem, AppSettings, VideoInfo
├── Services/        # DownloadManager, YtDlpService, DatabaseService, 
│                    # ClipboardMonitorService, SchedulerService,
│                    # BrowserExtensionServer, UpdateService
├── ViewModels/      # MainViewModel (MVVM pattern)
├── Views/           # MainWindow, AddDownloadDialog, SettingsWindow,
│                    # VideoDetectedPopup, BatchDownloadDialog
├── Converters/      # WPF value converters
├── Browser/         # Chrome/Edge extension (Manifest V3)
├── Installer/       # Inno Setup script
├── Resources/       # yt-dlp.exe, aria2c.exe, ffmpeg.exe
└── build.ps1        # PowerShell build script
```
