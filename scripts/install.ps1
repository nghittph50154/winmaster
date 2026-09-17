#Requires -Version 5.1
<#
.SYNOPSIS
    WinMaster Remote Installer Entry Point
.DESCRIPTION
    Entry point for remote WinMaster installation via:
    irm https://raw.githubusercontent.com/YOUR_USERNAME/WinMaster/main/install.ps1 | iex

    This script downloads and launches the WinMaster application.
    Source is always the official WinMaster GitHub repository.

.NOTES
    Version: 1.0.0
    Security: Only runs from the official WinMaster repository.
              DO NOT pipe untrusted scripts to iex.
#>

[CmdletBinding()]
param(
    [string]$RepoOwner   = "YOUR_USERNAME",   # Replace with your GitHub username
    [string]$RepoName    = "WinMaster",
    [string]$Branch      = "main",
    [string]$ReleaseTag  = "latest"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Configuration ────────────────────────────────────────────────────────────

$BaseUrl = "https://raw.githubusercontent.com/$RepoOwner/$RepoName/$Branch"
$ReleasesUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/$ReleaseTag"

# ─── Banner ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "  ██╗    ██╗██╗███╗   ██╗███╗   ███╗ █████╗ ███████╗████████╗███████╗██████╗ " -ForegroundColor Cyan
Write-Host "  ██║    ██║██║████╗  ██║████╗ ████║██╔══██╗██╔════╝╚══██╔══╝██╔════╝██╔══██╗" -ForegroundColor Cyan
Write-Host "  ██║ █╗ ██║██║██╔██╗ ██║██╔████╔██║███████║███████╗   ██║   █████╗  ██████╔╝" -ForegroundColor Cyan
Write-Host "  ██║███╗██║██║██║╚██╗██║██║╚██╔╝██║██╔══██║╚════██║   ██║   ██╔══╝  ██╔══██╗" -ForegroundColor Cyan
Write-Host "  ╚███╔███╔╝██║██║ ╚████║██║ ╚═╝ ██║██║  ██║███████║   ██║   ███████╗██║  ██║" -ForegroundColor Cyan
Write-Host "   ╚══╝╚══╝ ╚═╝╚═╝  ╚═══╝╚═╝     ╚═╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚══════╝╚═╝  ╚═╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  WinMaster v1.0.0 — Windows Software Installer" -ForegroundColor White
Write-Host "  Repository: $RepoOwner/$RepoName" -ForegroundColor DarkGray
Write-Host ""

# ─── Security notice ─────────────────────────────────────────────────────────

Write-Host "[SECURITY] This script will download and run WinMaster from:" -ForegroundColor Yellow
Write-Host "  https://github.com/$RepoOwner/$RepoName" -ForegroundColor Yellow
Write-Host ""

# ─── Check prerequisites ──────────────────────────────────────────────────────

Write-Host "[1/3] Checking system requirements..." -ForegroundColor Cyan

# Check Windows
if (-not $IsWindows -and $PSVersionTable.PSVersion.Major -ge 6) {
    Write-Error "WinMaster requires Windows. Current OS is not supported."
    exit 1
}

# Check internet
try {
    $null = Invoke-WebRequest -Uri "https://api.github.com" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Host "  ✓ Internet connection: OK" -ForegroundColor Green
} catch {
    Write-Error "Internet connection unavailable. Please check your network."
    exit 1
}

# ─── Download WinMaster ───────────────────────────────────────────────────────

Write-Host "[2/3] Downloading WinMaster..." -ForegroundColor Cyan

$DownloadDir = Join-Path $env:USERPROFILE "Downloads\WinMaster"
New-Item -ItemType Directory -Force -Path $DownloadDir | Out-Null

try {
    # Get latest release info from GitHub API
    $headers = @{ "User-Agent" = "WinMaster-Installer/1.0" }
    $release = Invoke-RestMethod -Uri $ReleasesUrl -Headers $headers -ErrorAction Stop

    $asset = $release.assets | Where-Object { $_.name -like "WinMaster-*.exe" } | Select-Object -First 1

    if ($null -eq $asset) {
        Write-Warning "No release executable found. Please download manually from:"
        Write-Host "  https://github.com/$RepoOwner/$RepoName/releases" -ForegroundColor Cyan
        exit 0
    }

    $exePath = Join-Path $DownloadDir $asset.name
    Write-Host "  Downloading: $($asset.name)" -ForegroundColor Gray

    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $exePath `
        -Headers $headers -UseBasicParsing -ErrorAction Stop

    Write-Host "  ✓ Downloaded to: $exePath" -ForegroundColor Green
} catch {
    Write-Warning "Could not fetch release automatically: $_"
    Write-Host ""
    Write-Host "  Please download WinMaster manually from:" -ForegroundColor Yellow
    Write-Host "  https://github.com/$RepoOwner/$RepoName/releases" -ForegroundColor Cyan
    exit 0
}

# ─── Launch WinMaster ────────────────────────────────────────────────────────

Write-Host "[3/3] Launching WinMaster..." -ForegroundColor Cyan
Start-Process -FilePath $exePath
Write-Host "  ✓ WinMaster launched." -ForegroundColor Green
Write-Host ""
Write-Host "  Enjoy WinMaster!" -ForegroundColor Cyan
