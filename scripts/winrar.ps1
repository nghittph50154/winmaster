# WinRAR Auto Activation Script
$ErrorActionPreference = 'SilentlyContinue'
$ProgressPreference = 'SilentlyContinue'

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "       WinRAR Activation Tool             " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$keyContent = @"
RAR registration data
State Grid Corporation Of China
50000 PC usage license
UID=5827a0bd1c43525d0a5d
64122122500a5d3d56f784f3a440ac3fb632d34e08bbaa37fc7712
6acaeb8eb044810272e86042cb7c79b1da0eaf88c79f8a7c6dd77b
dba335e27a109997ac90fb0e10e4129e79f46c42b4ee1832fa5113
7443fcc1124840d4dd36f3af84a5c915a760b18c6394f938168227
fbf29edbc4b34ef85ee53fbfca71814a82afadf073876b4b033451
b6292a7cc7975b3ff3cc73404abbf7c126787344169eeae4609f62
c9ffbc159bf2640ad5d9b88f8fa9d9cbf2b7e5b022a21938465244
"@

# Function to search for WinRAR directory across all possible locations
function Find-WinRARDirectory {
    # 1. Registry: App Paths
    $appPaths = @(
        (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinRAR.exe" -ErrorAction SilentlyContinue).'(default)',
        (Get-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinRAR.exe" -ErrorAction SilentlyContinue).'(default)'
    )
    foreach ($p in $appPaths) {
        if ($p -and (Test-Path $p)) {
            return (Split-Path -Parent $p)
        }
    }

    # 2. Registry: Uninstall keys
    $regUninstall = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )
    foreach ($reg in $regUninstall) {
        $found = Get-ItemProperty $reg -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -like "*WinRAR*" -or $_.PSChildName -like "*WinRAR*" } | Select-Object -First 1
        if ($found -and $found.InstallLocation -and (Test-Path $found.InstallLocation)) {
            return $found.InstallLocation
        }
    }

    # 3. Standard Program Files paths and other drives
    $commonPaths = @(
        "${env:ProgramFiles}\WinRAR",
        "${env:ProgramFiles(x86)}\WinRAR",
        "$env:LOCALAPPDATA\Programs\WinRAR",
        "C:\WinRAR",
        "D:\Program Files\WinRAR",
        "D:\WinRAR",
        "E:\Program Files\WinRAR",
        "E:\WinRAR"
    )
    foreach ($cp in $commonPaths) {
        if (Test-Path "$cp\WinRAR.exe") {
            return $cp
        }
    }

    # 4. PATH search
    $cmd = Get-Command winrar -ErrorAction SilentlyContinue
    if ($cmd -and (Test-Path $cmd.Source)) {
        return (Split-Path -Parent $cmd.Source)
    }

    return $null
}

# 1. First, always activate in %APPDATA%\WinRAR (WinRAR's official user license directory)
$appDataWinRAR = "$env:APPDATA\WinRAR"
if (-not (Test-Path $appDataWinRAR)) {
    New-Item -ItemType Directory -Path $appDataWinRAR -Force | Out-Null
}
Set-Content -Path "$appDataWinRAR\rarreg.key" -Value $keyContent -Force
Write-Host "[✓] Da ghi ban quyen vao: $appDataWinRAR\rarreg.key" -ForegroundColor Green

# 2. Locate WinRAR installation directory
$winrarDir = Find-WinRARDirectory

if (-not $winrarDir) {
    Write-Host "[!] Chua tim thay phan mem WinRAR da cai dat tren may." -ForegroundColor Yellow
    
    # Check if user has WinRAR installer in Downloads or Desktop
    $downloads = (New-Object -ComObject Shell.Application).Namespace('shell:Downloads').Self.Path
    if (-not $downloads) { $downloads = "$env:USERPROFILE\Downloads" }
    
    $localInstaller = Get-ChildItem -Path @($downloads, "$env:USERPROFILE\Desktop") -Filter "winrar*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($localInstaller) {
        Write-Host ">>> Phat hien bo cai: $($localInstaller.FullName). Dang tien hanh cai dat..." -ForegroundColor Cyan
        Start-Process -FilePath $localInstaller.FullName -ArgumentList "/S" -Wait
    } else {
        Write-Host ">>> Dang tu dong tai va cai dat WinRAR 64-bit ban quyen moi nhat tu rarlab.com..." -ForegroundColor Cyan
        $tempInstaller = "$env:TEMP\winrar-installer.exe"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
        Invoke-WebRequest -Uri "https://www.rarlab.com/rar/winrar-x64-701.exe" -OutFile $tempInstaller -UseBasicParsing
        if (Test-Path $tempInstaller) {
            Start-Process -FilePath $tempInstaller -ArgumentList "/S" -Wait
            Remove-Item $tempInstaller -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Re-detect after install
    Start-Sleep -Seconds 1
    $winrarDir = Find-WinRARDirectory
}

# 3. If WinRAR directory is found, copy rarreg.key there too
if ($winrarDir -and (Test-Path $winrarDir)) {
    Set-Content -Path "$winrarDir\rarreg.key" -Value $keyContent -Force
    Write-Host "[✓] Da ghi ban quyen vao: $winrarDir\rarreg.key" -ForegroundColor Green
}

# 4. Verify activation
$activated = (Test-Path "$appDataWinRAR\rarreg.key") -or ($winrarDir -and (Test-Path "$winrarDir\rarreg.key"))

Write-Host ""
if ($activated) {
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "      KICH HOAT WINRAR THANH CONG!        " -ForegroundColor Green
    Write-Host "   Ban quyen: 50,000 PC Usage License     " -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
} else {
    Write-Host "X Co loi trong qua trinh kich hoat WinRAR." -ForegroundColor Red
}

Write-Host ""
Pause
