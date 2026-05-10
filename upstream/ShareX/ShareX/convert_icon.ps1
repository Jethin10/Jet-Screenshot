# Regenerate the native ShareX fork branding assets from the shared app logo.
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$sourceFile = Join-Path $repoRoot "assets\app-logo.png"

if (-not (Test-Path $sourceFile)) {
    throw "Source logo not found: $sourceFile"
}

$iconOutputs = @(
    (Join-Path $PSScriptRoot "ShareX_Icon.ico"),
    (Join-Path $repoRoot "upstream\ShareX\ShareX.HelpersLib\Resources\ShareX_Icon.ico"),
    (Join-Path $repoRoot "upstream\ShareX\ShareX.HelpersLib\Resources\ShareX_Icon_White.ico"),
    (Join-Path $repoRoot "upstream\ShareX\ShareX.ImageEditor.App\Assets\ShareX_ImageEditor_Icon.ico")
)

$pngOutputs = @(
    @{ Path = (Join-Path $PSScriptRoot "Resources\About_Logo.png"); Size = 512 },
    @{ Path = (Join-Path $PSScriptRoot "Resources\application-icon-large.png"); Size = 128 },
    @{ Path = (Join-Path $repoRoot "upstream\ShareX\ShareX.HelpersLib\Resources\ShareX_Logo.png"); Size = 512 }
)

$sourceBmp = [System.Drawing.Bitmap]::new($sourceFile)

function New-ResizedBitmap([System.Drawing.Bitmap]$bitmap, [int]$size) {
    $resized = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($resized)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.DrawImage($bitmap, 0, 0, $size, $size)
    $graphics.Dispose()
    return $resized
}

foreach ($pngOutput in $pngOutputs) {
    $bitmap = New-ResizedBitmap $sourceBmp $pngOutput.Size
    $bitmap.Save($pngOutput.Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)

$dirOffset = $ms.Position
foreach ($s in $sizes) {
    $bw.Write([byte[]]::new(16))
}

$imageDataList = @()

foreach ($s in $sizes) {
    $resized = New-ResizedBitmap $sourceBmp $s
    $pngStream = New-Object System.IO.MemoryStream
    $resized.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageDataList += @{
        Width = $s
        Data = $pngStream.ToArray()
        Offset = $ms.Position
    }
    $bw.Write($pngStream.ToArray())
    $pngStream.Dispose()
    $resized.Dispose()
}

$ms.Position = $dirOffset
foreach ($entry in $imageDataList) {
    $w = if ($entry.Width -ge 256) { 0 } else { $entry.Width }
    $bw.Write([byte]$w)
    $bw.Write([byte]$w)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$entry.Data.Length)
    $bw.Write([UInt32]$entry.Offset)
}

$iconBytes = $ms.ToArray()
foreach ($iconPath in $iconOutputs) {
    [System.IO.File]::WriteAllBytes($iconPath, $iconBytes)
}

$sourceBmp.Dispose()
$bw.Dispose()
$ms.Dispose()

Write-Host "Updated ShareX branding assets from $sourceFile"
