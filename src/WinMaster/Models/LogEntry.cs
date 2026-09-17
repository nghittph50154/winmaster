namespace WinMaster.Models;

/// <summary>
/// Type of log entry.
/// </summary>
public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error,
    Debug
}

/// <summary>
/// A single log entry in the install log panel.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Message { get; set; } = string.Empty;
    public string? ApplicationId { get; set; }

    /// <summary>Prefix icon for display.</summary>
    public string Icon => Level switch
    {
        LogLevel.Success => "✓",
        LogLevel.Warning => "⚠",
        LogLevel.Error => "✗",
        LogLevel.Debug => "◦",
        _ => "›"
    };

    public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Icon} {Message}";
}

/// <summary>
/// Real-time progress update during an install session.
/// </summary>
public class InstallProgress
{
    public string CurrentAppId { get; set; } = string.Empty;
    public string CurrentAppName { get; set; } = string.Empty;
    public InstallStatus CurrentStatus { get; set; } = InstallStatus.Pending;
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public double ProgressPercent => TotalCount > 0 ? (double)CurrentIndex / TotalCount * 100 : 0;
}
