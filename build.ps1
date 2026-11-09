$ErrorActionPreference = 'Stop'

# Keep this file pure ASCII (Windows PowerShell 5.1 reads .ps1 as ANSI).
# The Chinese exe name is assembled from code points instead.
$exeName = -join ([int[]](0x8D34, 0x56FE, 0x66FF, 0x6362, 0x5DE5, 0x5177) | ForEach-Object { [char]$_ })

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc  = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw 'csc.exe not found' }

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$out = Join-Path $dist ('DDS' + $exeName + '.exe')
$srcs = @(Join-Path $root 'src\Program.cs')
$ui = Join-Path $root 'src\Ui.cs'
if (Test-Path $ui) { $srcs += $ui }
$man = Join-Path $root 'app.manifest'
$ico = Join-Path $root 'app.ico'

$iconArg = @()
if (Test-Path $ico) { $iconArg = @('/win32icon:' + $ico) }

& $csc /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /warn:4 `
    /win32manifest:$man `
    @iconArg `
    /out:$out `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    $srcs

if ($LASTEXITCODE -ne 0) { throw "compile failed (exit $LASTEXITCODE)" }

$docs = Join-Path $root 'docs'
if (Test-Path $docs) { Get-ChildItem $docs -Filter *.txt | Copy-Item -Destination $dist -Force }

$fi = Get-Item $out
"OK  ->  " + $fi.FullName
"size ->  " + [math]::Round($fi.Length / 1KB, 1) + " KB"
