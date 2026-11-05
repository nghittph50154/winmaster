# IDM Activation Script (PowerShell - Fixed Version)
# Runs the activation steps from IASL.cmd directly via PowerShell with admin rights

$SRC_DIR = Join-Path $PSScriptRoot "src"
$DATA_FILE = Join-Path $SRC_DIR "data.bin"
$DATAHLP_FILE = Join-Path $SRC_DIR "dataHlp.bin"
$REGISTRY_FILE = Join-Path $SRC_DIR "registry.bin"
$EXTENSIONS_FILE = Join-Path $SRC_DIR "extensions.bin"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   Coporton IDM Activation Script v2.5.8" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Check admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[!] This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "    Right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    OR run this in an elevated PowerShell:" -ForegroundColor Yellow
    Write-Host "    Start-Process powershell -Verb RunAs -ArgumentList '-File ""$PSCommandPath""'" -ForegroundColor Green
    pause
    exit 1
}

# Get IDM installation path from registry
$ExePath = (Get-ItemProperty "HKCU:\SOFTWARE\DownloadManager" -Name ExePath -ErrorAction SilentlyContinue).ExePath
if (-not $ExePath) {
    Write-Host "[!] IDM installation not found in registry." -ForegroundColor Red
    pause
    exit 1
}

$IDM_DIR = Split-Path $ExePath -Parent
Write-Host "[+] IDM found at: $IDM_DIR" -ForegroundColor Green

# Verify source files
foreach ($f in @($DATA_FILE, $DATAHLP_FILE, $REGISTRY_FILE)) {
    if (-not (Test-Path $f)) {
        Write-Host "[!] Missing file: $f" -ForegroundColor Red
        pause
        exit 1
    }
}

# Show menu
function Show-Menu {
    Write-Host ""
    Write-Host "  ======================================================" -ForegroundColor Green
    Write-Host "    :                                                :" -ForegroundColor Green
    Write-Host "    :  [1] Download Latest IDM Version               :" -ForegroundColor Green
    Write-Host "    :  [2] Activate Internet Download Manager        :" -ForegroundColor Green
    Write-Host "    :  [3] Extra FileTypes Extensions                :" -ForegroundColor Green
    Write-Host "    :  [4] Do Everything (2 + 3)                     :" -ForegroundColor Green
    Write-Host "    :  [5] Completely Remove IDM Registry Entries    :" -ForegroundColor Red
    Write-Host "    :  [6] Exit                                       :" -ForegroundColor Green
    Write-Host "    :                                                :" -ForegroundColor Green
    Write-Host "  ======================================================" -ForegroundColor Green
    Write-Host ""
}

function Invoke-ActivateIDM {
    Write-Host ""
    Write-Host "[*] Terminating IDMan.exe if running..." -ForegroundColor Yellow
    Stop-Process -Name "IDMan" -Force -ErrorAction SilentlyContinue

    Write-Host "[*] Importing activation registry..." -ForegroundColor Yellow
    Start-Process -FilePath "regedit.exe" -ArgumentList "/s `"$REGISTRY_FILE`"" -Wait -WindowStyle Hidden

    Write-Host "[*] Copying activated IDMan.exe..." -ForegroundColor Yellow
    try {
        Copy-Item -Force $DATA_FILE "$IDM_DIR\IDMan.exe" -ErrorAction Stop
        Write-Host "[+] IDMan.exe replaced successfully." -ForegroundColor Green
    } catch {
        Write-Host "[!] Failed to copy IDMan.exe: $_" -ForegroundColor Red
        return
    }

    Write-Host "[*] Copying IDMGrHlp.exe..." -ForegroundColor Yellow
    try {
        Copy-Item -Force $DATAHLP_FILE "$IDM_DIR\IDMGrHlp.exe" -ErrorAction Stop
        Write-Host "[+] IDMGrHlp.exe replaced successfully." -ForegroundColor Green
    } catch {
        Write-Host "[!] Failed to copy IDMGrHlp.exe: $_" -ForegroundColor Red
    }

    # Update user info in registry
    $FName = Read-Host "Enter your First Name: "
    $LName = Read-Host "Enter your Last Name: "
    if ([string]::IsNullOrWhiteSpace($FName)) { $FName = "Phong" }
    if ([string]::IsNullOrWhiteSpace($LName)) { $LName = "AI" }

    Set-ItemProperty -Path "HKCU:\SOFTWARE\DownloadManager" -Name "FName" -Value $FName
    Set-ItemProperty -Path "HKCU:\SOFTWARE\DownloadManager" -Name "LName" -Value $LName

    Write-Host ""
    Write-Host "[+] Internet Download Manager Activated!" -ForegroundColor Green
    Write-Host "    Registered to: $FName $LName" -ForegroundColor Cyan
    Write-Host ""
}

function Invoke-AddExtensions {
    Write-Host "[*] Importing extra file type extensions..." -ForegroundColor Yellow
    Start-Process -FilePath "regedit.exe" -ArgumentList "/s `"$EXTENSIONS_FILE`"" -Wait -WindowStyle Hidden
    Write-Host "[+] Extra FileTypes Extensions updated." -ForegroundColor Green
}

function Invoke-CleanRegistry {
    Write-Host ""
    Write-Host "[*] Terminating IDMan.exe if running..." -ForegroundColor Yellow
    Stop-Process -Name "IDMan" -Force -ErrorAction SilentlyContinue

    Write-Host "[*] Cleaning IDM-related Registry Entries..." -ForegroundColor Yellow

    $keys = @(
        "HKLM:\Software\Classes\CLSID\{7B8E9164-324D-4A2E-A46D-0165FB2000EC}",
        "HKLM:\Software\Classes\CLSID\{6DDF00DB-1234-46EC-8356-27E7B2051192}",
        "HKLM:\Software\Classes\CLSID\{D5B91409-A8CA-4973-9A0B-59F713D25671}",
        "HKLM:\Software\Internet Download Manager",
        "HKLM:\Software\Wow6432Node\Internet Download Manager",
        "HKCU:\Software\Download Manager",
        "HKCU:\Software\Wow6432Node\Download Manager"
    )

    foreach ($k in $keys) {
        if (Test-Path $k) {
            Remove-Item -Path $k -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  Deleted: $k" -ForegroundColor DarkGray
        }
    }

    # Clean license values
    $licenseValues = @("FName","LName","Email","Serial","CheckUpdtVM","tvfrdt","LstCheck","scansk","idmvers")
    foreach ($v in $licenseValues) {
        Remove-ItemProperty -Path "HKCU:\Software\DownloadManager" -Name $v -ErrorAction SilentlyContinue
    }

    Write-Host "[+] Registry cleanup completed." -ForegroundColor Green
    Write-Host ""
}

# Main loop
while ($true) {
    Show-Menu
    $choice = Read-Host " Choose an option (1-6)"

    switch ($choice) {
        "1" {
            Write-Host "[*] Opening download page for latest IDM..." -ForegroundColor Cyan
            Start-Process "https://www.internetdownloadmanager.com/download.html"
        }
        "2" { Invoke-ActivateIDM }
        "3" { Invoke-AddExtensions }
        "4" {
            Invoke-ActivateIDM
            Invoke-AddExtensions
            Write-Host ""
            Write-Host "[+] All tasks completed successfully!" -ForegroundColor Green
        }
        "5" { Invoke-CleanRegistry }
        "6" {
            Write-Host ""
            Write-Host "Thank you for using Coporton IDM Activation Script. Have a great day!" -ForegroundColor Green
            Write-Host ""
            exit 0
        }
        default {
            Write-Host "[!] Invalid option. Please enter a number from 1 to 6." -ForegroundColor Red
        }
    }
}

