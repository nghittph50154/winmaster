# PowerShell online launcher script for WinMaster
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Auto elevate to Administrator if not already elevated
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    try {
        if ($PSCommandPath) {
            Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs -ErrorAction Stop
        } else {
            Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -Command `"irm https://raw.githubusercontent.com/nghittph50154/winmaster/main/run.ps1 | iex`"" -Verb RunAs -ErrorAction Stop
        }
        exit
    } catch {
        Write-Host "Notice: Administrator elevation not granted. Continuing in standard mode..." -ForegroundColor Yellow
    }
}

Write-Host "=== WinMaster Launcher ===" -ForegroundColor Cyan

$workDir = "$env:LOCALAPPDATA\WinMaster"
$exePath = "$workDir\App\WinMaster.exe"
$zipPath = "$workDir\WinMaster.zip"
$extractPath = "$workDir\App"
$versionFile = "$workDir\version.txt"
$etagFile = "$workDir\etag.txt"
$zipUrl = "https://github.com/nghittph50154/winmaster/raw/main/publish_out/WinMaster.zip"
$EXPECTED_VERSION = "1.1.5"
$DISPLAY_VERSION = "v$EXPECTED_VERSION"

if (-not (Test-Path $workDir)) {
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
}

if (Get-Command Add-MpPreference -ErrorAction SilentlyContinue) {
    Add-MpPreference -ExclusionPath $workDir -ErrorAction SilentlyContinue
}

# Fetch remote ETag to detect new builds even on the same version number
$remoteEtag = ""
try {
    $head = Invoke-WebRequest -Uri $zipUrl -Method Head -UseBasicParsing -ErrorAction SilentlyContinue
    $remoteEtag = ($head.Headers['ETag'] -join '').Trim()
} catch {}

function Stop-RunningWinMaster {
    $procs = Get-Process -Name "WinMaster" -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "Closing running WinMaster instance to apply update..." -ForegroundColor Yellow
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 600
    }
}

function Clean-AppDirectory {
    param([string]$Path)
    Stop-RunningWinMaster
    if (Test-Path $Path) {
        # Chỉ xóa các file và thư mục con bên trong, giữ nguyên thư mục gốc để tránh lỗi khóa thư mục của Windows
        Get-ChildItem -Path $Path -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Check version & ETag: if exe exists and is up to date, launch immediately
$needDownload = $true
if ((Test-Path $exePath) -and (Test-Path $versionFile)) {
    $localVersion = (Get-Content $versionFile -Raw).Trim()
    $localEtag = if (Test-Path $etagFile) { (Get-Content $etagFile -Raw).Trim() } else { "" }

    if ($localVersion -eq $EXPECTED_VERSION -and ($remoteEtag -eq "" -or $localEtag -eq $remoteEtag)) {
        $needDownload = $false
        Write-Host "WinMaster $DISPLAY_VERSION is up to date. Launching..." -ForegroundColor Cyan
    } else {
        Write-Host "Update detected for WinMaster $DISPLAY_VERSION. Downloading update..." -ForegroundColor Yellow
        Clean-AppDirectory $extractPath
        Remove-Item -Path $versionFile, $etagFile -Force -ErrorAction SilentlyContinue
    }
} elseif (Test-Path $exePath) {
    Write-Host "No version info found. Re-downloading latest WinMaster..." -ForegroundColor Yellow
    Clean-AppDirectory $extractPath
}

if (-not $needDownload) {
    Unblock-File -Path $exePath -ErrorAction SilentlyContinue
    & "$exePath"
    exit
}

$localZip = if ($PSScriptRoot) { Join-Path $PSScriptRoot "publish_out\WinMaster.zip" } else { $null }

if ($localZip -and (Test-Path $localZip)) {
    Write-Host "Installing WinMaster $DISPLAY_VERSION from local package..." -ForegroundColor Green
    Copy-Item -Path $localZip -Destination $zipPath -Force
} else {
    Write-Host "Downloading WinMaster $DISPLAY_VERSION..." -ForegroundColor Green
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
}

Write-Host "Extracting WinMaster..." -ForegroundColor Green
Clean-AppDirectory $extractPath
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

# Unblock all extracted files (remove internet Zone Identifier to allow execution)
Get-ChildItem -Path $extractPath -Recurse | Unblock-File -ErrorAction SilentlyContinue

# Save version and ETag files after successful download
Set-Content -Path $versionFile -Value $EXPECTED_VERSION
if ($remoteEtag) { Set-Content -Path $etagFile -Value $remoteEtag }

Write-Host "Launching WinMaster $DISPLAY_VERSION..." -ForegroundColor Cyan
& "$exePath"

