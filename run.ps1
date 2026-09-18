# PowerShell online launcher script for WinMaster
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Auto elevate to Administrator if not already elevated
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -Command `"irm https://raw.githubusercontent.com/nghittph50154/winmaster/main/run.ps1 | iex`"" -Verb RunAs
    exit
}

Write-Host "=== WinMaster Launcher ===" -ForegroundColor Cyan

# Check and auto-install winget if missing
$wingetAvailable = (Get-Command winget -ErrorAction SilentlyContinue) -ne $null
if (-not $wingetAvailable) {
    Write-Host "Winget not found. Installing Winget automatically..." -ForegroundColor Yellow
    try {
        # Download VCLibs dependency
        $vcLibsUrl = "https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx"
        $vcLibsPath = "$env:TEMP\VCLibs.appx"
        Invoke-WebRequest -Uri $vcLibsUrl -OutFile $vcLibsPath -UseBasicParsing -ErrorAction SilentlyContinue
        if (Test-Path $vcLibsPath) { Add-AppxPackage -Path $vcLibsPath -ErrorAction SilentlyContinue }

        # Download and install winget
        $wingetUrl = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
        $wingetInstaller = "$env:TEMP\Microsoft.DesktopAppInstaller.msixbundle"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $wingetUrl -OutFile $wingetInstaller -UseBasicParsing
        Add-AppxPackage -Path $wingetInstaller -ErrorAction SilentlyContinue

        # Refresh PATH to pick up newly installed winget
        $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH","User")

        # Also try direct path to winget.exe
        $wingetExe = "$env:LOCALAPPDATA\Microsoft\WindowsApps\winget.exe"
        if (Test-Path $wingetExe) {
            $env:PATH = "$env:PATH;$env:LOCALAPPDATA\Microsoft\WindowsApps"
            $wingetAvailable = $true
            Write-Host "Winget installed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Winget was installed. Please re-run this script for it to take effect." -ForegroundColor Yellow
            # Delete cached WinMaster so it re-downloads fresh version next run
            $oldExe = "$env:LOCALAPPDATA\WinMaster\App\WinMaster.exe"
            if (Test-Path $oldExe) { Remove-Item $oldExe -Force -ErrorAction SilentlyContinue }
            pause
            exit
        }
    } catch {
        Write-Host "Could not auto-install Winget: $_" -ForegroundColor Red
    }
}

$workDir = "$env:LOCALAPPDATA\WinMaster"
$exePath = "$workDir\App\WinMaster.exe"
$zipPath = "$workDir\WinMaster.zip"
$extractPath = "$workDir\App"
$zipUrl = "https://github.com/nghittph50154/winmaster/raw/main/publish_out/WinMaster.zip"

if (-not (Test-Path $workDir)) {
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
}

if (Get-Command Add-MpPreference -ErrorAction SilentlyContinue) {
    Add-MpPreference -ExclusionPath $workDir -ErrorAction SilentlyContinue
}

# If WinMaster.exe exists, launch instantly! If missing, download fresh copy
if (Test-Path $exePath) {
    Write-Host "Launching WinMaster..." -ForegroundColor Cyan
    Start-Process -FilePath $exePath
    exit
}

Write-Host "Downloading WinMaster standalone application..." -ForegroundColor Green
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing

Write-Host "Extracting WinMaster..." -ForegroundColor Green
if (Test-Path $extractPath) { Remove-Item -Path $extractPath -Recurse -Force }
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

Write-Host "Launching WinMaster..." -ForegroundColor Cyan
Start-Process -FilePath $exePath
