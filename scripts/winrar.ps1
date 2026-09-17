# WinRAR Auto Activation Script
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

Write-Host "=== WinRAR Activation ===" -ForegroundColor Cyan

# Find WinRAR installation directory
$winrarPath = "${env:ProgramFiles}\WinRAR"
if (-not (Test-Path $winrarPath)) { 
    $winrarPath = "${env:ProgramFiles(x86)}\WinRAR" 
}

if (-not (Test-Path $winrarPath)) {
    Write-Host "X Khong tim thay thu muc cai dat WinRAR tren may!" -ForegroundColor Red
    Write-Host "Vui long cai dat WinRAR truoc khi kich hoat." -ForegroundColor Yellow
    Pause
    exit
}

$keyUrl = "https://raw.githubusercontent.com/nghittph50154/winmaster/main/sources/WinRAR/rarreg.key"
$targetKeyPath = "$winrarPath\rarreg.key"

Write-Host "Dang tai file ban quyen rarreg.key..." -ForegroundColor Green
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $keyUrl -OutFile $targetKeyPath -UseBasicParsing

if (Test-Path $targetKeyPath) {
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host " KICH HOAT WINRAR THANH CONG! " -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
} else {
    Write-Host "X Activation failed." -ForegroundColor Red
}
