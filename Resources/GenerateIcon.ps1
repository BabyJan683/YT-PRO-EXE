<#
.SYNOPSIS
    Generates app.ico for YT Downloader Pro using System.Drawing.
    Red circle with white play triangle + download arrow. Multi-size ICO (256/128/64/48/32/16).
.PARAMETER OutputPath
    Where to write the .ico file. Default: same folder as this script\app.ico
#>
param([string]$OutputPath = "")

if (!$OutputPath) {
    $OutputPath = Join-Path $PSScriptRoot "app.ico"
}

Add-Type -AssemblyName System.Drawing

$sizes = @(256, 128, 64, 48, 32, 16)
$frames = [System.Collections.Generic.List[System.Drawing.Bitmap]]::new()

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    # ── Background: dark rounded square ─────────────────────────────────────
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 18, 18, 18))
    $g.FillRectangle($bgBrush, 0, 0, $sz, $sz)

    # ── Red circle ───────────────────────────────────────────────────────────
    $pad = [int]($sz * 0.06)
    $redBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 220, 32, 32))
    $g.FillEllipse($redBrush, $pad, $pad, $sz - $pad*2, $sz - $pad*2)

    # Subtle inner highlight on circle top
    $hlBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Point]::new(0, $pad),
        [System.Drawing.Point]::new(0, $sz/2),
        [System.Drawing.Color]::FromArgb(60, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillEllipse($hlBrush, $pad, $pad, $sz - $pad*2, $sz - $pad*2)

    # ── White play triangle (centred, slightly left) ─────────────────────────
    $cx = [float]($sz * 0.48)
    $cy = [float]($sz * 0.50)
    $r  = [float]($sz * 0.26)
    $playPts = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($cx - $r*0.55, $cy - $r),
        [System.Drawing.PointF]::new($cx + $r,       $cy),
        [System.Drawing.PointF]::new($cx - $r*0.55, $cy + $r)
    )
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillPolygon($whiteBrush, $playPts)

    # ── Download arrow badge (bottom-right) ──────────────────────────────────
    if ($sz -ge 32) {
        $ax  = [float]($sz * 0.60)
        $ay  = [float]($sz * 0.58)
        $aw  = [float]($sz * 0.32)
        $ah  = [float]($sz * 0.24)
        $lw  = [float]($sz * 0.055)

        # Arrow shaft
        $arrowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $lw)
        $arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $arrowPen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawLine($arrowPen, $ax + $aw/2, $ay, $ax + $aw/2, $ay + $ah * 0.6)

        # Arrow head
        $headPts = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new($ax,           $ay + $ah * 0.5),
            [System.Drawing.PointF]::new($ax + $aw/2,   $ay + $ah),
            [System.Drawing.PointF]::new($ax + $aw,     $ay + $ah * 0.5)
        )
        $g.FillPolygon($whiteBrush, $headPts)

        # Horizontal base line
        $basePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $lw)
        $basePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $basePen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawLine($basePen, $ax, $ay + $ah + $lw/2, $ax + $aw, $ay + $ah + $lw/2)
    }

    $g.Dispose()
    $frames.Add($bmp)
}

# ── Write multi-size ICO file manually ───────────────────────────────────────
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# ICO header: reserved=0, type=1 (icon), count
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$frames.Count)

# Build PNG data for each frame
$pngs = [System.Collections.Generic.List[byte[]]]::new()
foreach ($bmp in $frames) {
    $tmp = New-Object System.IO.MemoryStream
    $bmp.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs.Add($tmp.ToArray())
}

# Directory entries (16 bytes each)
$offset = 6 + 16 * $frames.Count
for ($i = 0; $i -lt $frames.Count; $i++) {
    $bmp  = $frames[$i]
    $data = $pngs[$i]
    $w    = if ($bmp.Width  -eq 256) { 0 } else { $bmp.Width }
    $h    = if ($bmp.Height -eq 256) { 0 } else { $bmp.Height }
    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0)        # color count (0 = not palette)
    $bw.Write([byte]0)        # reserved
    $bw.Write([uint16]1)      # color planes
    $bw.Write([uint16]32)     # bits per pixel
    $bw.Write([uint32]$data.Length)
    $bw.Write([uint32]$offset)
    $offset += $data.Length
}

# Image data
foreach ($data in $pngs) { $bw.Write($data) }
$bw.Flush()

[System.IO.File]::WriteAllBytes($OutputPath, $ms.ToArray())

foreach ($bmp in $frames) { $bmp.Dispose() }

Write-Host "Icon written: $OutputPath ($([math]::Round((Get-Item $OutputPath).Length/1KB,1)) KB, $($sizes.Count) sizes)" -ForegroundColor Green
