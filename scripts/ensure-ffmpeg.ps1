param(
    [Parameter(Mandatory = $true)]
    [string]$TargetDirectory
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$targetDirectory = (Resolve-Path -LiteralPath $TargetDirectory).Path
$ffmpegExePath = Join-Path $targetDirectory "ffmpeg.exe"

if (Test-Path -LiteralPath $ffmpegExePath) {
    Write-Host "FFmpeg already exists at: $ffmpegExePath"
    return
}

$apiUrl = "https://api.github.com/repos/ShareX/FFmpeg/releases/latest"
$headers = @{ "User-Agent" = "Jet-Screenshot-Release" }

Write-Host "Fetching FFmpeg release metadata..."
$release = Invoke-RestMethod -Uri $apiUrl -Headers $headers

$assetName = if ([Environment]::Is64BitOperatingSystem) { "ffmpeg-*-win-x64.zip" } else { "ffmpeg-*-win32.zip" }
$asset = $release.assets | Where-Object { $_.name -like $assetName } | Select-Object -First 1

if (-not $asset) {
    throw "No FFmpeg asset matched '$assetName' in release '$($release.tag_name)'."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("jetsnap-ffmpeg-" + [Guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $tempRoot $asset.name
$extractDir = Join-Path $tempRoot "extract"

New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
New-Item -ItemType Directory -Force -Path $extractDir | Out-Null

try {
    Write-Host "Downloading $($asset.name)..."
    Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -OutFile $zipPath

    Write-Host "Extracting FFmpeg..."
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force

    $downloadedFfmpeg = Get-ChildItem -LiteralPath $extractDir -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1

    if (-not $downloadedFfmpeg) {
        throw "Downloaded archive did not contain ffmpeg.exe."
    }

    Copy-Item -LiteralPath $downloadedFfmpeg.FullName -Destination $ffmpegExePath -Force
    Write-Host "Bundled FFmpeg to: $ffmpegExePath"
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
