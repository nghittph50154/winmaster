# PowerShell online launcher script for WinMaster
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Auto elevate to Administrator if not already elevated
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -Command `"irm https://raw.githubusercontent.com/nghittph50154/winmaster/main/run.ps1 | iex`"" -Verb RunAs
    exit
}

Write-Host "=== WinMaster Launcher ===" -ForegroundColor Cyan

# Function to test if winget is truly executable and functional
function Test-WingetFunctional {
    # 1. Check direct executable in WindowsApps folder (fastest and bypasses alias restrictions)
    try {
        $dirs = Get-Item "C:\Program Files\WindowsApps\Microsoft.DesktopAppInstaller_*" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending
        foreach ($d in $dirs) {
            $candidate = Join-Path $d.FullName "winget.exe"
            if (Test-Path $candidate) {
                $p = Start-Process -FilePath $candidate -ArgumentList "--version" -NoNewWindow -PassThru -Wait -ErrorAction Stop
                if ($p.ExitCode -eq 0) {
                    $env:PATH = "$($d.FullName);$env:PATH"
                    return $true
                }
            }
        }
    } catch {}

    # 2. Try standard PATH / winget command
    try {
        $p = Start-Process -FilePath "winget" -ArgumentList "--version" -NoNewWindow -PassThru -Wait -ErrorAction Stop
        if ($p.ExitCode -eq 0) { return $true }
    } catch {}

    return $false
}

# Check and install winget if missing or broken before launching WinMaster
Write-Host ">>> Kiểm tra Windows Package Manager (Winget)..." -ForegroundColor Cyan
if (-not (Test-WingetFunctional)) {
    Write-Host "Winget chưa sẵn sàng hoặc bị lỗi. Đang tiến hành kích hoạt / cài đặt Winget..." -ForegroundColor Yellow

    # Try registering existing provisioned package first (fastest)
    try {
        Get-AppxPackage -AllUsers *DesktopAppInstaller* -ErrorAction SilentlyContinue | ForEach-Object {
            Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml" -ErrorAction SilentlyContinue
        }
    } catch {}

    # If still not functional, download and install dependencies + DesktopAppInstaller
    if (-not (Test-WingetFunctional)) {
        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13

            # 1. Download & Install VCLibs
            Write-Host "  [1/3] Đang tải Microsoft VCLibs x64..." -ForegroundColor Gray
            $vcLibsUrl = "https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx"
            $vcLibsPath = "$env:TEMP\Microsoft.VCLibs.x64.appx"
            Invoke-WebRequest -Uri $vcLibsUrl -OutFile $vcLibsPath -UseBasicParsing -ErrorAction SilentlyContinue
            if (Test-Path $vcLibsPath) {
                Add-AppxPackage -Path $vcLibsPath -ErrorAction SilentlyContinue
            }

            # 2. Download & Install UI.Xaml 2.8
            Write-Host "  [2/3] Đang tải Microsoft UI Xaml 2.8..." -ForegroundColor Gray
            $uiXamlUrl = "https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx"
            $uiXamlPath = "$env:TEMP\Microsoft.UI.Xaml.2.8.x64.appx"
            Invoke-WebRequest -Uri $uiXamlUrl -OutFile $uiXamlPath -UseBasicParsing -ErrorAction SilentlyContinue
            if (Test-Path $uiXamlPath) {
                Add-AppxPackage -Path $uiXamlPath -ErrorAction SilentlyContinue
            }

            # 3. Download & Install DesktopAppInstaller (Winget)
            Write-Host "  [3/3] Đang tải Microsoft DesktopAppInstaller (Winget)..." -ForegroundColor Gray
            $wingetUrl = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
            $wingetInstaller = "$env:TEMP\Microsoft.DesktopAppInstaller.msixbundle"
            Invoke-WebRequest -Uri $wingetUrl -OutFile $wingetInstaller -UseBasicParsing
            if (Test-Path $wingetInstaller) {
                # Try system-wide provisioned install (works under Administrator)
                try {
                    Add-AppxProvisionedPackage -Online -PackagePath $wingetInstaller -DependencyPackagePath @($vcLibsPath, $uiXamlPath) -SkipLicense -ErrorAction SilentlyContinue | Out-Null
                } catch {}
                Add-AppxPackage -Path $wingetInstaller -DependencyPath $vcLibsPath, $uiXamlPath -ErrorAction SilentlyContinue
            }

            # Refresh PATH
            $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH","User") + ";$env:LOCALAPPDATA\Microsoft\WindowsApps"

            # Clean up temp installers
            Remove-Item $vcLibsPath, $uiXamlPath, $wingetInstaller -Force -ErrorAction SilentlyContinue
        } catch {
            Write-Host "Lỗi trong quá trình cài đặt Winget: $_" -ForegroundColor Red
        }
    }

    # Re-check after installation
    if (Test-WingetFunctional) {
        Write-Host "✅ Cài đặt Winget thành công!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Chưa thể kích hoạt Winget tự động. WinMaster vẫn sẽ mở (bạn có thể dùng các app tải trực tiếp)." -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ Winget đã sẵn sàng trên hệ thống." -ForegroundColor Green
}

$workDir = "$env:LOCALAPPDATA\WinMaster"
$exePath = "$workDir\App\WinMaster.exe"
$zipPath = "$workDir\WinMaster.zip"
$extractPath = "$workDir\App"
$versionFile = "$workDir\version.txt"
$etagFile = "$workDir\etag.txt"
$zipUrl = "https://github.com/nghittph50154/winmaster/raw/main/publish_out/WinMaster.zip"
$EXPECTED_VERSION = "1.1.3"

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

# Check version & ETag: if exe exists and is up to date, launch immediately
$needDownload = $true
if ((Test-Path $exePath) -and (Test-Path $versionFile)) {
    $localVersion = (Get-Content $versionFile -Raw).Trim()
    $localEtag = if (Test-Path $etagFile) { (Get-Content $etagFile -Raw).Trim() } else { "" }

    if ($localVersion -eq $EXPECTED_VERSION -and ($remoteEtag -eq "" -or $localEtag -eq $remoteEtag)) {
        $needDownload = $false
        Write-Host "WinMaster $EXPECTED_VERSION is up to date. Launching..." -ForegroundColor Cyan
    } else {
        Write-Host "Update detected for WinMaster $EXPECTED_VERSION. Downloading update..." -ForegroundColor Yellow
        Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $versionFile, $etagFile -Force -ErrorAction SilentlyContinue
    }
} elseif (Test-Path $exePath) {
    Write-Host "No version info found. Re-downloading latest WinMaster..." -ForegroundColor Yellow
    Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not $needDownload) {
    Unblock-File -Path $exePath -ErrorAction SilentlyContinue
    & "$exePath"
    exit
}

Write-Host "Downloading WinMaster $EXPECTED_VERSION..." -ForegroundColor Green
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing

Write-Host "Extracting WinMaster..." -ForegroundColor Green
if (Test-Path $extractPath) { Remove-Item -Path $extractPath -Recurse -Force }
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

# Unblock all extracted files (remove internet Zone Identifier to allow execution)
Get-ChildItem -Path $extractPath -Recurse | Unblock-File -ErrorAction SilentlyContinue

# Save version and ETag files after successful download
Set-Content -Path $versionFile -Value $EXPECTED_VERSION
if ($remoteEtag) { Set-Content -Path $etagFile -Value $remoteEtag }

Write-Host "Launching WinMaster $EXPECTED_VERSION..." -ForegroundColor Cyan
& "$exePath"

