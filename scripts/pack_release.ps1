# Script to package publish_out into WinMaster.zip
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishOut = Join-Path $repoRoot "publish_out"
$zipPath = Join-Path $publishOut "WinMaster.zip"
$tempZip = Join-Path $repoRoot "WinMaster_temp.zip"

Write-Host "Publish output directory: $publishOut" -ForegroundColor Cyan

if (Test-Path $tempZip) {
    Remove-Item $tempZip -Force
}

$items = Get-ChildItem -Path $publishOut | Where-Object { $_.Name -ne "WinMaster.zip" }
Write-Host "Found $($items.Count) top-level items to compress." -ForegroundColor Cyan

Compress-Archive -Path $items.FullName -DestinationPath $tempZip -CompressionLevel Optimal
Write-Host "Created temp zip successfully." -ForegroundColor Green

Move-Item -Path $tempZip -Destination $zipPath -Force
Write-Host "Updated $zipPath successfully!" -ForegroundColor Green

$zipInfo = Get-Item $zipPath
Write-Host "Final WinMaster.zip size: $([math]::Round($zipInfo.Length / 1MB, 2)) MB" -ForegroundColor Green
