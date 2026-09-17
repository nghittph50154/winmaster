using System.Collections.ObjectModel;
using WinMaster.Models;

namespace WinMaster.Services;

/// <summary>
/// Manages log entries for the current installation session.
/// Thread-safe for use from background tasks.
/// </summary>
public class LogService
{
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = new();

    public event EventHandler<LogEntry>? EntryAdded;

    /// <summary>Read-only snapshot of all log entries.</summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public void Log(string message, LogLevel level = LogLevel.Info, string? appId = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            ApplicationId = appId
        };

        lock (_lock)
        {
            _entries.Add(entry);
        }

        EntryAdded?.Invoke(this, entry);
    }

    public void Info(string message, string? appId = null) => Log(message, LogLevel.Info, appId);
    public void Success(string message, string? appId = null) => Log(message, LogLevel.Success, appId);
    public void Warning(string message, string? appId = null) => Log(message, LogLevel.Warning, appId);
    public void Error(string message, string? appId = null) => Log(message, LogLevel.Error, appId);
    public void Debug(string message, string? appId = null) => Log(message, LogLevel.Debug, appId);

    public void Clear()
    {
        lock (_lock) { _entries.Clear(); }
    }
}
