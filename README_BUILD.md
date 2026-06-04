# YT Downloader Pro v2.0 — Build Instructions

## What's Inside

```
YTDownloaderPro/
├── build.ps1                  ← ONE-CLICK BUILD SCRIPT (run this)
├── YTDownloaderPro.csproj
├── App.xaml / App.xaml.cs
├── Resources/
│   ├── GenerateIcon.ps1       ← Auto-generates app.ico (red play + download arrow)
│   └── app.ico                ← Generated on first build
├── Installer/
│   └── setup.iss              ← Complete Inno Setup installer script
├── Views/  Services/  Models/ ViewModels/  ...
└── Output/                    ← Installer EXE appears here after build
```

---

## Requirements (installed automatically if missing)

| Tool | Required | Auto-install? |
|---|---|---|
| Windows 10/11 x64 | ✅ | — |
| .NET 8 SDK | ✅ | ✅ Yes, via build.ps1 |
| Inno Setup 6 | For installer | ✅ Auto-downloads |
| yt-dlp / aria2 / ffmpeg | For downloads | ✅ via `-DownloadDeps` flag |

---

## Quick Start

Open **PowerShell** (not cmd), navigate to this folder, then:

### Option A — Full build (first time, downloads everything)
```powershell
cd YTDownloaderPro
.\build.ps1 -DownloadDeps -CreateInstaller
```
This will:
1. Install .NET 8 SDK if missing
2. Generate `Resources\app.ico` (red YT logo with download arrow)
3. Download yt-dlp, aria2, ffmpeg into `Resources\`
4. Compile `YTDownloaderPro.exe` (single-file, ~30–60 MB)
5. Run Inno Setup → produce `Output\YTDownloaderPro_v2.0.0_Setup.exe`

### Option B — Rebuild only (deps already present)
```powershell
.\build.ps1 -CreateInstaller
```

### Option C — Just compile, no installer
```powershell
.\build.ps1
```

---

## What the Installer Does

When the user runs `YTDownloaderPro_v2.0.0_Setup.exe`:

1. **Welcome** page with app name + version
2. **License Agreement** (add your own `LICENSE.txt` if needed)
3. **Select Destination Folder** — default: `C:\Program Files\YT Downloader Pro`  
   User can click Browse and choose any folder
4. **Installation Mode** — Standard (AppData) or Portable (all data in install folder)
5. **Select Start Menu Folder** — default: `YT Downloader Pro`
6. **Additional Tasks**:
   - ☐ Create desktop shortcut
   - ☐ **Launch YT Downloader Pro when Windows starts** ← startup option
   - ☑ Show icon in system tray
   - ☑ Install browser extension helper
7. **Ready to Install** summary
8. **Installing…** progress bar
9. **Finish** — optionally launches the app

---

## Features in the Built EXE

| Feature | Status |
|---|---|
| App icon on taskbar & title bar | ✅ |
| System tray icon (red YT logo) | ✅ |
| Tray right-click menu | ✅ Open / Add Download / Pause All / Settings / Exit |
| Tray double-click to open | ✅ |
| Minimize to tray | ✅ |
| Close to tray | ✅ |
| Start with Windows | ✅ (registry key, toggle from tray menu or Settings) |
| YouTube video detected → popup | ✅ (browser extension + clipboard monitor) |
| 7-day trial | ✅ auto-starts on first run |
| Product key activation | ✅ online (Supabase) + offline fallback |

---

## Troubleshooting

**"dotnet" is not recognized**  
→ Re-open PowerShell after .NET SDK installs, or add `%LOCALAPPDATA%\Microsoft\dotnet` to your PATH.

**Build error: `net8.0-windows` not available**  
→ Make sure you installed the Windows Desktop SDK, not just the base .NET runtime.  
   Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0 → SDK → Windows x64

**Inno Setup not found**  
→ `build.ps1` tries to download it silently. If that fails, install manually:  
   https://jrsoftware.org/isdl.php → Inno Setup 6

**Tray icon shows as blank/default**  
→ The icon is loaded from `Resources\app.ico` next to the EXE.  
   Run `.\Resources\GenerateIcon.ps1` to regenerate it if missing.

**App opens but yt-dlp download fails**  
→ Run `.\build.ps1 -DownloadDeps` to fetch the latest yt-dlp into `Resources\`.

---

## Distributing

After a successful build you get:

```
Output\
└── YTDownloaderPro_v2.0.0_Setup.exe   ← Send this to users
```

Users just double-click, click Next → Next → Next → Finish, and the app works.

---

## YouTube Video Popup

The popup works two ways:

1. **Browser Extension** — Install the Chrome/Edge extension from `Browser\` folder.  
   When you play a video on YouTube, the extension sends the URL to the app and the popup appears.

2. **Clipboard Monitor** — Copy any YouTube URL (`Ctrl+C`) while the app is running  
   and the popup will automatically appear.

Both can be toggled in Settings → General.
