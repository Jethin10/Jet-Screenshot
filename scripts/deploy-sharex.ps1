$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "upstream\ShareX\ShareX\ShareX.csproj"
$buildOutputDir = Join-Path $root "upstream\ShareX\ShareX\bin\Release\win-x64"
$builtExe = Join-Path $buildOutputDir "ShareX.exe"
$destDir = Join-Path $root "release"

Write-Host "Stopping ShareX if running..."
Get-Process -Name "ShareX" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

Write-Host "Building Release win-x64..."
dotnet build $project -c Release -r win-x64 | Out-Host

if (-not (Test-Path $builtExe)) {
    throw "Build output not found: $builtExe"
}

New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Get-ChildItem -LiteralPath $destDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Get-ChildItem -LiteralPath $buildOutputDir -Force | Copy-Item -Destination $destDir -Recurse -Force

Write-Host "Deployed to: $destDir"
Write-Host "Build output: $buildOutputDir"
