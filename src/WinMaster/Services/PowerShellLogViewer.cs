using System.Diagnostics;
using System.IO;
using System.Text;
using WinMaster.Models;

namespace WinMaster.Services;

/// <summary>
/// Manages streaming logs to an external live PowerShell window.
/// Similar to WinUtil, detailed execution output is displayed in a dedicated PowerShell console.
/// </summary>
public class PowerShellLogViewer
{
    private readonly string _logFilePath;
    private readonly object _fileLock = new();
    private Process? _powershellProcess;

    public PowerShellLogViewer()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinMaster");
        Directory.CreateDirectory(dir);
        _logFilePath = Path.Combine(dir, "session.log");
    }

    /// <summary>
    /// Writes a log entry into the session log file (monitored by the live PowerShell window).
    /// </summary>
    public void WriteEntry(LogEntry entry)
    {
        try
        {
            var line = $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level.ToString().ToUpper()}] {entry.Icon} {entry.Message}";
            lock (_fileLock)
            {
                using var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.WriteLine(line);
            }
        }
        catch
        {
            // Logging must never crash the application
        }
    }

    /// <summary>
    /// Clears the session log file for a fresh session.
    /// </summary>
    public void ResetLog()
    {
        try
        {
            lock (_fileLock)
            {
                using var stream = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] ─── WinMaster Session Initialized ───");
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Opens the PowerShell log window if it is not already running.
    /// </summary>
    public void EnsureOpen()
    {
        try
        {
            if (_powershellProcess is not null && !_powershellProcess.HasExited)
            {
                return;
            }

            // Ensure the log file exists before starting PowerShell tail
            lock (_fileLock)
            {
                if (!File.Exists(_logFilePath))
                {
                    File.WriteAllText(_logFilePath, $"[{DateTime.Now:HH:mm:ss}] [INFO] ─── WinMaster Session Started ───\r\n", Encoding.UTF8);
                }
            }

            var currentPid = Environment.ProcessId;
            var escapedPath = _logFilePath.Replace("'", "''");
            var script = $@"[CmdletBinding()]
param([int]$ParentPid = 0)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = 'WinMaster - PowerShell Activity Log'
Clear-Host
Write-Host '================================================================' -ForegroundColor Cyan
Write-Host '               WINMASTER - POWERSHELL ACTIVITY LOG              ' -ForegroundColor Yellow
Write-Host '================================================================' -ForegroundColor Cyan
Write-Host 'Cua so PowerShell dang lang nghe va hien thi log thoi gian thuc...' -ForegroundColor DarkGray
Write-Host ''

if ($ParentPid -gt 0) {{
    $timer = New-Object System.Timers.Timer
    $timer.Interval = 1000
    Register-ObjectEvent -InputObject $timer -EventName Elapsed -Action {{
        $parent = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
        if (-not $parent) {{
            [System.Environment]::Exit(0)
        }}
    }} | Out-Null
    $timer.Start()
}}

Get-Content -Path '{escapedPath}' -Wait -Tail 30 -Encoding UTF8 | ForEach-Object {{
    if ($_ -match '\[SUCCESS\]|✓|Hoàn thành|Success') {{
        Write-Host $_ -ForegroundColor Green
    }} elseif ($_ -match '\[ERROR\]|✗|Lỗi|Failed') {{
        Write-Host $_ -ForegroundColor Red
    }} elseif ($_ -match '\[WARNING\]|⚠|Warning') {{
        Write-Host $_ -ForegroundColor Yellow
    }} elseif ($_ -match '───|===') {{
        Write-Host $_ -ForegroundColor Cyan
    }} elseif ($_ -match '\[DEBUG\]') {{
        Write-Host $_ -ForegroundColor DarkGray
    }} else {{
        Write-Host $_ -ForegroundColor White
    }}
}}
";

            var scriptFile = Path.Combine(Path.GetDirectoryName(_logFilePath)!, "watch_log.ps1");
            File.WriteAllText(scriptFile, script, Encoding.UTF8);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\" -ParentPid {currentPid}",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            _powershellProcess = Process.Start(psi);
        }
        catch
        {
            // Fallback: continue silently if PowerShell window cannot be opened
        }
    }

    /// <summary>
    /// Stops and terminates the external PowerShell log viewer process.
    /// </summary>
    public void Close()
    {
        try
        {
            if (_powershellProcess is not null && !_powershellProcess.HasExited)
            {
                _powershellProcess.Kill(entireProcessTree: true);
                _powershellProcess.Dispose();
                _powershellProcess = null;
            }
        }
        catch
        {
        }
    }
}
