# PowerShell online launcher script for WinMaster
$ErrorActionPreference = 'Stop'

# Disable PowerShell GUI progress bar (speeds up Invoke-WebRequest by 10x!)
$ProgressPreference = 'SilentlyContinue'

# Auto elevate to Administrator if not already elevated
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Requesting Administrator privileges..." -ForegroundColor Yellow
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -Command `"irm https://raw.githubusercontent.com/nghittph50154/winmaster/main/run.ps1 | iex`"" -Verb RunAs
    exit
}

Write-Host "=== WinMaster Launcher ===" -ForegroundColor Cyan

# Working Directory in AppData / Temp
$workDir = "$env:LOCALAPPDATA\WinMaster"
if (-not (Test-Path $workDir)) {
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
}

# Add exclusion to Defender to avoid false positive blocks
Add-MpPreference -ExclusionPath $workDir -ErrorAction SilentlyContinue

$zipPath = "$workDir\WinMaster.zip"
$extractPath = "$workDir\App"
$zipUrl = "https://github.com/nghittph50154/winmaster/archive/refs/heads/main.zip"

Write-Host "Downloading WinMaster latest release..." -ForegroundColor Green
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing

Write-Host "Extracting files..." -ForegroundColor Green
if (Test-Path $extractPath) { Remove-Item -Path $extractPath -Recurse -Force }
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

# Locate WinMaster workspace or run via dotnet if installed
Set-Location -Path "$extractPath\winmaster-main"

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "Launching WinMaster..." -ForegroundColor Cyan
    dotnet run --project "src/WinMaster/WinMaster.csproj"
} else {
    Write-Host "Dotnet SDK not found. Please install .NET 10 runtime." -ForegroundColor Red
}
