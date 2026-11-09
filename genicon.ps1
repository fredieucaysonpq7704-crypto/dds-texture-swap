$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$tmp  = Join-Path $root '_test\icon_tmp'
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

function New-RoundRect([double]$x, [double]$y, [double]$w, [double]$h, [double]$r) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = 2 * $r
  $p.AddArc($x, $y, $d, $d, 180, 90)
  $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
  $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
  $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
  $p.CloseFigure()
  $p
}

function Save-IconPng([int]$size, [string]$file) {
  $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
  $g.Clear([System.Drawing.Color]::Transparent)

  $pad  = [double]$size * 0.03
  $side = [double]$size - 2 * $pad
  $path = New-RoundRect $pad $pad $side $side ([double]$size * 0.22)

  $c1 = [System.Drawing.Color]::FromArgb(255, 37, 99, 235)
  $c2 = [System.Drawing.Color]::FromArgb(255, 6, 182, 212)
  $rect = New-Object System.Drawing.RectangleF([float]$pad, [float]$pad, [float]$side, [float]$side)
  $gb = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, [float]45)
  $g.FillPath($gb, $path)

  # 两片轻微错开的"贴图"，暗示 A 替换 B
  if ($size -ge 24) {
    $pw = [double]$size * 0.40
    $ph = [double]$size * 0.26
    $x1 = [double]$size * 0.13
    $y1 = [double]$size * 0.14
    $penW = [float]([math]::Max(1.0, [double]$size * 0.032))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(110, 255, 255, 255), $penW)
    $g.DrawRectangle($pen, [float]$x1, [float]$y1, [float]$pw, [float]$ph)
    $g.DrawRectangle($pen, [float]($x1 + $size * 0.10), [float]($y1 + $size * 0.10), [float]$pw, [float]$ph)
  }

  $fs = [float]([double]$size * 0.30)
  if ($size -le 20) { $fs = [float]([double]$size * 0.44) }
  $font = New-Object System.Drawing.Font('Segoe UI', $fs, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
  $sf = New-Object System.Drawing.StringFormat
  $sf.Alignment = [System.Drawing.StringAlignment]::Center
  $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
  $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
  $box = New-Object System.Drawing.RectangleF([float]0, [float]($size * 0.34), [float]$size, [float]($size * 0.52))
  $g.DrawString('DDS', $font, $white, $box, $sf)

  $g.Dispose()
  $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose()
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$files = @()
foreach ($s in $sizes) {
  $f = Join-Path $tmp ('i' + $s + '.png')
  Save-IconPng $s $f
  $files += $f
  "  png " + $s + "px -> " + (Get-Item $f).Length + " bytes"
}

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Count)

$offset = 6 + 16 * $sizes.Count
$blobs = @()
foreach ($f in $files) {
  $b = [System.IO.File]::ReadAllBytes($f)
  $blobs += , $b
}
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $s = $sizes[$i]
  $dim = if ($s -ge 256) { 0 } else { $s }
  $bw.Write([byte]$dim)
  $bw.Write([byte]$dim)
  $bw.Write([byte]0)
  $bw.Write([byte]0)
  $bw.Write([uint16]1)
  $bw.Write([uint16]32)
  $bw.Write([uint32]$blobs[$i].Length)
  $bw.Write([uint32]$offset)
  $offset += $blobs[$i].Length
}
foreach ($b in $blobs) { $bw.Write([byte[]]$b) }
$bw.Flush()

$out = Join-Path $root 'app.ico'
[System.IO.File]::WriteAllBytes($out, $ms.ToArray())
"icon -> $out  ($([math]::Round((Get-Item $out).Length/1KB,1)) KB, $($sizes.Count) sizes)"
