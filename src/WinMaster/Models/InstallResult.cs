namespace WinMaster.Models;

/// <summary>
/// Overall status of an installation.
/// </summary>
public enum InstallStatus
{
    Pending,
    Downloading,
    Installing,
    Verifying,
    Success,
    Failed,
    Skipped
}

/// <summary>
/// Result of installing a single application.
/// </summary>
public class InstallResult
{
    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public InstallStatus Status { get; set; } = InstallStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int? ExitCode { get; set; }
    public string? VerificationDetail { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.Now;

    public bool IsSuccess => Status == InstallStatus.Success;

    /// <summary>Human-readable summary of result.</summary>
    public string Summary => Status switch
    {
        InstallStatus.Success => $"✓ {ApplicationName} installed successfully",
        InstallStatus.Failed => $"✗ {ApplicationName} failed: {ErrorMessage}",
        InstallStatus.Skipped => $"⚠ {ApplicationName} skipped",
        _ => $"? {ApplicationName}"
    };
}

/// <summary>
/// Summary after completing all installations in a session.
/// </summary>
public class InstallSessionSummary
{
    public List<InstallResult> Results { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }

    public int SuccessCount => Results.Count(r => r.IsSuccess);
    public int FailedCount => Results.Count(r => r.Status == InstallStatus.Failed);
    public int SkippedCount => Results.Count(r => r.Status == InstallStatus.Skipped);
    public int TotalCount => Results.Count;
    public TimeSpan TotalDuration => CompletedAt - StartedAt;

    public List<InstallResult> FailedResults => Results.Where(r => r.Status == InstallStatus.Failed).ToList();
}
