@echo off
:: Launcher: Run ActivateIDM.ps1 with Administrator privileges
:: Just double-click this file!

set "SCRIPT=%~dp0ActivateIDM.ps1"

:: Check if already admin
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    PowerShell -Command "Start-Process powershell -ArgumentList '-ExecutionPolicy Bypass -File ""%SCRIPT%""' -Verb RunAs"
    exit /b
)

:: Already admin, run directly
PowerShell -ExecutionPolicy Bypass -File "%SCRIPT%"

