#Requires -Version 5.1
<#
.SYNOPSIS
    YT Downloader Pro — One-Click Build & Installer Script
.DESCRIPTION
    1. Installs .NET 8 SDK if missing
    2. Downloads yt-dlp, aria2, ffmpeg into Resources\
    3. Generates app.ico
    4. Compiles single-file EXE (win-x64)
    5. Creates professional Inno Setup installer
.EXAMPLE
    # Full build (recommended first time):
    .\build.ps1 -DownloadDeps -CreateInstaller

    # Rebuild only (deps already present):
    .\build.ps1 -CreateInstaller
#>

param(
    [string]$Configuration = "Release",
    [switch]$DownloadDeps,
    [switch]$CreateInstaller,
    [string]$InnoSetupPath = ""
)

$ErrorActionPreference = "Stop"
$Version     = "2.0.0"
$ProjectRoot = $PSScriptRoot
$ResDir      = Join-Path $ProjectRoot "Resources"
$OutDir      = Join-Path (Split-Path $ProjectRoot) "Output"
$PublishDir  = "$ProjectRoot\bin\$Configuration\net8.0-windows\win-x64\publish"

function Title  { Write-Host "`n══ $args ══" -ForegroundColor Cyan }
function OK     { Write-Host "  ✓ $args" -ForegroundColor Green }
function Warn   { Write-Host "  ! $args" -ForegroundColor Yellow }
function Fail   { Write-Host "  ✗ $args" -ForegroundColor Red; exit 1 }

Clear-Host
Write-Host @"
  ╔═══════════════════════════════════════════╗
  ║      YT Downloader Pro  v$Version           ║
  ║      Professional Build & Package Tool    ║
  ╚═══════════════════════════════════════════╝
"@ -ForegroundColor Red

# ── Ensure running on Windows ─────────────────────────────────────────────────
if ($env:OS -ne "Windows_NT") { Fail "This script must run on Windows." }

New-Item -ItemType Directory -Path $ResDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# ── Step 1: Install .NET 8 SDK if missing ────────────────────────────────────
Title "Step 1/5 — .NET SDK"
$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($dotnet) {
    $v = (dotnet --version 2>&1)
    OK ".NET SDK $v found"
} else {
    Warn ".NET 8 SDK not found — downloading installer..."
    $sdkUrl = "https://dot.net/v1/dotnet-install.ps1"
    $sdkScript = Join-Path $env:TEMP "dotnet-install.ps1"
    Invoke-WebRequest $sdkUrl -OutFile $sdkScript -UseBasicParsing
    & $sdkScript -Channel 8.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
    $env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
    OK ".NET 8 SDK installed"
}

# ── Step 2: Generate App Icon ─────────────────────────────────────────────────
Title "Step 2/5 — Application Icon"
$iconPath = Join-Path $ResDir "app.ico"
if (!(Test-Path $iconPath)) {
    $iconScript = Join-Path $ResDir "GenerateIcon.ps1"
    if (Test-Path $iconScript) {
        & $iconScript -OutputPath $iconPath
    } else {
        # Inline icon generation using System.Drawing
        Add-Type -AssemblyName System.Drawing
        $sizes = @(256, 128, 64, 48, 32, 16)
        $frames = [System.Collections.Generic.List[System.Drawing.Bitmap]]::new()
        foreach ($sz in $sizes) {
            $bmp = New-Object System.Drawing.Bitmap($sz, $sz)
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            # Background: dark gradient
            $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                [System.Drawing.Point]::new(0,0),
                [System.Drawing.Point]::new($sz,$sz),
                [System.Drawing.Color]::FromArgb(255,20,20,20),
                [System.Drawing.Color]::FromArgb(255,40,40,40))
            $g.FillRectangle($brush, 0, 0, $sz, $sz)
            # Red circle background
            $redBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,220,30,30))
            $margin = [int]($sz * 0.05)
            $g.FillEllipse($redBrush, $margin, $margin, $sz-$margin*2, $sz-$margin*2)
            # White play triangle
            $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
            $cx = $sz / 2; $cy = $sz / 2; $r = $sz * 0.28
            $pts = @(
                [System.Drawing.PointF]::new([float]($cx - $r*0.6), [float]($cy - $r)),
                [System.Drawing.PointF]::new([float]($cx + $r),      [float]$cy),
                [System.Drawing.PointF]::new([float]($cx - $r*0.6), [float]($cy + $r))
            )
            $g.FillPolygon($whiteBrush, $pts)
            # Small download arrow at bottom-right
            $arrowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200,255,255,255))
            $ax = [int]($sz * 0.72); $ay = [int]($sz * 0.62); $aw = [int]($sz * 0.22); $ah = [int]($sz * 0.3)
            $arrowPts = @(
                [System.Drawing.PointF]::new([float]($ax),            [float]($ay)),
                [System.Drawing.PointF]::new([float]($ax + $aw),      [float]($ay)),
                [System.Drawing.PointF]::new([float]($ax + $aw/2),    [float]($ay + $ah))
            )
            $g.FillPolygon($arrowBrush, $arrowPts)
            $g.DrawLine(New-Object System.Drawing.Pen([System.Drawing.Color]::White, [float]($sz*0.05)),
                [float]($ax + $aw/2), [float]($ay + $ah),
                [float]($ax + $aw/2), [float]($ay + $ah + $sz*0.08))
            $g.Dispose()
            $frames.Add($bmp)
        }
        # Write ICO manually (multi-size)
        $ms = New-Object System.IO.MemoryStream
        $bw = New-Object System.IO.BinaryWriter($ms)
        # ICO header
        $bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$frames.Count)
        $dataOffset = 6 + 16 * $frames.Count
        $imageData  = [System.Collections.Generic.List[byte[]]]::new()
        foreach ($bmp in $frames) {
            $tmp = New-Object System.IO.MemoryStream
            $bmp.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
            $bytes = $tmp.ToArray()
            $imageData.Add($bytes)
            $w = if ($bmp.Width -eq 256) { 0 } else { $bmp.Width }
            $h = if ($bmp.Height -eq 256) { 0 } else { $bmp.Height }
            $bw.Write([byte]$w); $bw.Write([byte]$h)
            $bw.Write([byte]0); $bw.Write([byte]0)
            $bw.Write([uint16]1); $bw.Write([uint16]32)
            $bw.Write([uint32]$bytes.Length)
            $bw.Write([uint32]$dataOffset)
            $dataOffset += $bytes.Length
        }
        foreach ($data in $imageData) { $bw.Write($data) }
        $bw.Flush()
        [System.IO.File]::WriteAllBytes($iconPath, $ms.ToArray())
        foreach ($bmp in $frames) { $bmp.Dispose() }
        OK "app.ico generated ($([math]::Round((Get-Item $iconPath).Length/1KB,1)) KB)"
    }
} else { OK "app.ico already present" }

# Copy icon to root for manifest
Copy-Item $iconPath (Join-Path $ProjectRoot "Resources\app.ico") -Force -ErrorAction SilentlyContinue

# ── Step 3: Download Dependencies ─────────────────────────────────────────────
if ($DownloadDeps) {
    Title "Step 3/5 — Download Dependencies"

    # yt-dlp
    Write-Host "  Downloading yt-dlp..." -ForegroundColor Gray -NoNewline
    try {
        Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" `
            -OutFile (Join-Path $ResDir "yt-dlp.exe") -UseBasicParsing -TimeoutSec 120
        OK "yt-dlp"
    } catch { Warn "yt-dlp download failed: $_" }

    # aria2
    Write-Host "  Downloading aria2..." -ForegroundColor Gray -NoNewline
    try {
        $rel   = Invoke-RestMethod "https://api.github.com/repos/aria2/aria2/releases/latest" -TimeoutSec 30
        $asset = $rel.assets | Where-Object { $_.name -match "win-64.*\.zip$" } | Select-Object -First 1
        if ($asset) {
            $zip = "$env:TEMP\aria2.zip"; $ext = "$env:TEMP\aria2_ext"
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing -TimeoutSec 120
            Expand-Archive $zip $ext -Force
            $exe = Get-ChildItem $ext -Filter "aria2c.exe" -Recurse | Select-Object -First 1
            if ($exe) { Copy-Item $exe.FullName (Join-Path $ResDir "aria2c.exe") -Force }
            Remove-Item $zip,$ext -Recurse -Force -ErrorAction SilentlyContinue
            OK "aria2c"
        }
    } catch { Warn "aria2 download failed" }

    # FFmpeg
    Write-Host "  Downloading FFmpeg (may take ~1 min)..." -ForegroundColor Gray -NoNewline
    try {
        $rel   = Invoke-RestMethod "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest" -TimeoutSec 30
        $asset = $rel.assets | Where-Object { $_.name -match "win64.*gpl.*\.zip$" -and $_.name -notmatch "shared" } | Select-Object -First 1
        if ($asset) {
            $zip = "$env:TEMP\ffmpeg.zip"; $ext = "$env:TEMP\ffmpeg_ext"
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing -TimeoutSec 300
            Expand-Archive $zip $ext -Force
            foreach ($name in @("ffmpeg.exe","ffprobe.exe")) {
                $f = Get-ChildItem $ext -Filter $name -Recurse | Select-Object -First 1
                if ($f) { Copy-Item $f.FullName (Join-Path $ResDir $name) -Force }
            }
            Remove-Item $zip,$ext -Recurse -Force -ErrorAction SilentlyContinue
            OK "ffmpeg + ffprobe"
        }
    } catch { Warn "FFmpeg download failed" }
} else {
    Title "Step 3/5 — Dependencies"
    foreach ($f in @("yt-dlp.exe","aria2c.exe","ffmpeg.exe")) {
        $p = Join-Path $ResDir $f
        if (Test-Path $p) { OK "$f present" } else { Warn "$f missing (run with -DownloadDeps)" }
    }
}

# ── Step 4: Build EXE ─────────────────────────────────────────────────────────
Title "Step 4/5 — Compile Application"
Write-Host "  Running dotnet publish..." -ForegroundColor Gray

$buildArgs = @(
    "publish", "$ProjectRoot\YTDownloaderPro.csproj",
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained", "false",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=none",
    "-p:DebugSymbols=false",
    "-o", $PublishDir,
    "--nologo"
)

& dotnet @buildArgs 2>&1 | ForEach-Object {
    $line = $_.ToString()
    if ($line -match " error ") { Write-Host "    $line" -ForegroundColor Red }
    elseif ($line -match " warning ") { Write-Host "    $line" -ForegroundColor Yellow }
    elseif ($line -notmatch "^\s*$") { Write-Host "    $line" -ForegroundColor DarkGray }
}

if ($LASTEXITCODE -ne 0) { Fail "Compile failed! See errors above." }

$exePath = Join-Path $PublishDir "YTDownloaderPro.exe"
if (!(Test-Path $exePath)) { Fail "EXE not produced at: $exePath" }
$sz = [math]::Round((Get-Item $exePath).Length/1MB, 1)
OK "YTDownloaderPro.exe  ($sz MB)  →  $PublishDir"

# Copy resources next to EXE
$pubRes = Join-Path $PublishDir "Resources"
New-Item -ItemType Directory -Path $pubRes -Force | Out-Null
Get-ChildItem $ResDir -File | Copy-Item -Destination $pubRes -Force
$browserSrc = Join-Path $ProjectRoot "Browser"
if (Test-Path $browserSrc) { Copy-Item $browserSrc (Join-Path $PublishDir "Browser") -Recurse -Force }
OK "Resources + Browser extension copied"

# ── Step 5: Create Installer ──────────────────────────────────────────────────
if ($CreateInstaller) {
    Title "Step 5/5 — Create Installer"

    # Find ISCC.exe
    if (!$InnoSetupPath -or !(Test-Path $InnoSetupPath)) {
        $candidates = @(
            "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
            "C:\Program Files\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        )
        foreach ($c in $candidates) { if (Test-Path $c) { $InnoSetupPath = $c; break } }
    }

    if (!(Test-Path $InnoSetupPath)) {
        Warn "Inno Setup 6 not found. Installing silently..."
        try {
            $isUrl = "https://jrsoftware.org/download.php/is.exe"
            $isInst = "$env:TEMP\innosetup.exe"
            Invoke-WebRequest $isUrl -OutFile $isInst -UseBasicParsing -TimeoutSec 120
            Start-Process $isInst -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait
            $InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
            if (!(Test-Path $InnoSetupPath)) { $InnoSetupPath = "C:\Program Files\Inno Setup 6\ISCC.exe" }
        } catch { Warn "Inno Setup auto-install failed. Download manually: https://jrsoftware.org/isdl.php" }
    }

    if (Test-Path $InnoSetupPath) {
        Write-Host "  Compiling installer script..." -ForegroundColor Gray
        & $InnoSetupPath (Join-Path $ProjectRoot "Installer\setup.iss") /Q
        if ($LASTEXITCODE -eq 0) {
            $ins = Get-ChildItem $OutDir -Filter "*Setup*.exe" | Sort-Object LastWriteTime | Select-Object -Last 1
            if ($ins) {
                $isz = [math]::Round($ins.Length/1MB, 1)
                OK "$($ins.Name)  ($isz MB)"
                OK "Location: $OutDir"
            }
        } else { Warn "Installer compile failed — check Installer\setup.iss" }
    } else { Warn "Inno Setup not available. Installer not created." }
} else {
    Title "Step 5/5 — Installer"
    Warn "Skipped. Re-run with: .\build.ps1 -CreateInstaller"
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  ╔══════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "  ║               BUILD SUCCESSFUL  ✓               ║" -ForegroundColor Green
Write-Host "  ╚══════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  EXE:       $exePath" -ForegroundColor White
if ($CreateInstaller) {
    Write-Host "  Installer: $OutDir\YTDownloaderPro_v${Version}_Setup.exe" -ForegroundColor White
}
Write-Host "  Run the EXE directly or distribute the installer." -ForegroundColor Gray
Write-Host ""
