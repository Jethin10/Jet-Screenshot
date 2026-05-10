$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "upstream\\ShareX\\ShareX\\ShareX.csproj"
$exe = Join-Path $root "upstream\\ShareX\\ShareX\\bin\\Release\\win-x64\\ShareX.exe"

Write-Host "Building ShareX overlay build..."
dotnet build $project -c Release -r win-x64 | Out-Host

if (-not (Test-Path $exe)) {
    throw "ShareX executable not found at $exe"
}

Write-Host "Launching ShareX overlay build..."
Start-Process -FilePath $exe
