using WinMaster.Infrastructure;
using WinMaster.Installers;
using WinMaster.Models;

namespace WinMaster.Services;

/// <summary>
/// Orchestrates sequential installation of multiple applications.
/// 
/// Flow:
///   Pre-flight checks → ForEach(app): Resolve → Download → Install → Verify → Log → Next
/// 
/// Key behaviors:
///   - Sequential (not parallel) in v1.0
///   - One app failing does NOT stop the queue
///   - Clear progress reporting via events
///   - Admin check per app if required
/// </summary>
public class InstallerEngine
{
    private readonly List<IInstaller> _installers;
    private readonly VerificationService _verificationService;
    private readonly LogService _logService;
    private readonly NetworkService _networkService;
    private readonly AdminService _adminService;

    public event EventHandler<InstallProgress>? ProgressChanged;
    public event EventHandler<InstallResult>? AppCompleted;
    public event EventHandler<InstallSessionSummary>? SessionCompleted;

    public InstallerEngine(
        WingetInstaller wingetInstaller,
        DirectInstaller directInstaller,
        FixedSourceInstaller fixedInstaller,
        PowerShellInstaller psInstaller,
        PendingInstaller pendingInstaller,
        VerificationService verificationService,
        LogService logService,
        NetworkService networkService,
        AdminService adminService)
    {
        _installers = [wingetInstaller, directInstaller, fixedInstaller, psInstaller, pendingInstaller];
        _verificationService = verificationService;
        _logService = logService;
        _networkService = networkService;
        _adminService = adminService;
    }

    private bool _isRunning;
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Runs the sequential installation of all selected applications.
    /// </summary>
    public async Task<InstallSessionSummary> RunAsync(
        IReadOnlyList<ApplicationEntry> selectedApps,
        string? customInstallPath = null,
        bool isInteractive = false,
        CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            throw new InvalidOperationException("An installation session is already in progress.");

        _isRunning = true;
        var summary = new InstallSessionSummary { StartedAt = DateTime.Now };

        try
        {
            _logService.Clear();
            _logService.Info("WinMaster Installation Session Started");
            _logService.Info($"Preparing to install {selectedApps.Count} application(s)...");

            // Pre-flight: check internet for apps that need it
            var needsInternet = selectedApps.Any(a =>
                a.Installer.InstallerTypeEnum is InstallerType.Winget or InstallerType.Direct or InstallerType.PowerShell);

            if (needsInternet)
            {
                _logService.Info("Checking internet connection...");
                ReportProgress(new InstallProgress
                {
                    StatusMessage = "Checking internet connection...",
                    TotalCount = selectedApps.Count
                });

                var hasInternet = await _networkService.IsInternetAvailableAsync();
                if (!hasInternet)
                {
                    _logService.Warning("Internet connection unavailable.");
                    _logService.Warning("Applications requiring a download will be skipped.");
                }
                else
                {
                    _logService.Info("Internet connection: OK");
                }

                // If all apps need internet and we don't have it, abort
                if (!hasInternet && selectedApps.All(a =>
                    a.Installer.InstallerTypeEnum is InstallerType.Winget or InstallerType.Direct or InstallerType.PowerShell))
                {
                    _logService.Error("Internet connection unavailable. Cannot proceed with installation.");
                    summary.CompletedAt = DateTime.Now;
                    foreach (var app in selectedApps)
                    {
                        summary.Results.Add(new InstallResult
                        {
                            ApplicationId = app.Id,
                            ApplicationName = app.DisplayName,
                            Status = InstallStatus.Skipped,
                            ErrorMessage = "Internet connection unavailable."
                        });
                    }
                    SessionCompleted?.Invoke(this, summary);
                    return summary;
                }
            }

            // Main installation loop — sequential
            for (int i = 0; i < selectedApps.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logService.Warning("Installation cancelled by user.");
                    break;
                }

                var app = selectedApps[i];

                ReportProgress(new InstallProgress
                {
                    CurrentAppId = app.Id,
                    CurrentAppName = app.DisplayName,
                    CurrentStatus = InstallStatus.Pending,
                    CurrentIndex = i + 1,
                    TotalCount = selectedApps.Count,
                    StatusMessage = $"Installing {app.DisplayName}..."
                });

                _logService.Info($"─── [{i + 1}/{selectedApps.Count}] Installing {app.DisplayName} ───", app.Id);

                var result = await InstallSingleAppAsync(app, i, selectedApps.Count, customInstallPath, isInteractive, cancellationToken);
                summary.Results.Add(result);
                AppCompleted?.Invoke(this, result);

                _logService.Log(result.Summary, result.IsSuccess ? LogLevel.Success : LogLevel.Error, app.Id);

                // Small delay between apps for stability
                if (i < selectedApps.Count - 1)
                    await Task.Delay(500, cancellationToken);
            }

            // Final summary
            summary.CompletedAt = DateTime.Now;
            _logService.Info("─────────────────────────────────────");
            _logService.Info($"Installation Session Complete");
            _logService.Info($"Success: {summary.SuccessCount} | Failed: {summary.FailedCount} | Skipped: {summary.SkippedCount}");

            if (summary.FailedResults.Any())
            {
                _logService.Warning("Failed applications:");
                foreach (var failed in summary.FailedResults)
                    _logService.Error($"  • {failed.ApplicationName}: {failed.ErrorMessage}");
            }

            SessionCompleted?.Invoke(this, summary);
        }
        catch (OperationCanceledException)
        {
            _logService.Warning("Installation was cancelled.");
            summary.CompletedAt = DateTime.Now;
            SessionCompleted?.Invoke(this, summary);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected engine error: {ex.Message}");
            summary.CompletedAt = DateTime.Now;
            SessionCompleted?.Invoke(this, summary);
        }
        finally
        {
            _isRunning = false;
        }

        return summary;
    }

    private async Task<InstallResult> InstallSingleAppAsync(
        ApplicationEntry app,
        int index,
        int total,
        string? customInstallPath,
        bool isInteractive,
        CancellationToken cancellationToken)
    {
        // Find the right installer
        var installer = _installers.FirstOrDefault(i => i.CanHandle(app));
        if (installer is null)
        {
            _logService.Error($"No installer found for {app.DisplayName} (type: {app.Installer.Type})");
            return new InstallResult
            {
                ApplicationId = app.Id,
                ApplicationName = app.DisplayName,
                Status = InstallStatus.Failed,
                ErrorMessage = $"No installer available for type: {app.Installer.Type}"
            };
        }

        // Check admin if needed
        if (app.RequiresAdmin && !_adminService.IsRunningAsAdmin())
        {
            _logService.Warning($"{app.DisplayName} may require Administrator privileges.");
            // Don't block — some installers handle UAC internally (winget, EXE with UAC manifest)
        }

        // Output progress relay
        var outputProgress = new Progress<string>(line =>
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _logService.Debug(line, app.Id);
                ReportProgress(new InstallProgress
                {
                    CurrentAppId = app.Id,
                    CurrentAppName = app.DisplayName,
                    CurrentStatus = InstallStatus.Installing,
                    CurrentIndex = index + 1,
                    TotalCount = total,
                    StatusMessage = line.Length > 80 ? line[..80] + "…" : line
                });
            }
        });

        InstallResult result;

        try
        {
            ReportProgress(new InstallProgress
            {
                CurrentAppId = app.Id,
                CurrentAppName = app.DisplayName,
                CurrentStatus = InstallStatus.Downloading,
                CurrentIndex = index + 1,
                TotalCount = total,
                StatusMessage = $"Preparing {app.DisplayName}..."
            });

            result = await installer.InstallAsync(app, customInstallPath, isInteractive, outputProgress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error($"Installer threw exception for {app.DisplayName}: {ex.Message}");
            return new InstallResult
            {
                ApplicationId = app.Id,
                ApplicationName = app.DisplayName,
                Status = InstallStatus.Failed,
                ErrorMessage = ex.Message,
                CompletedAt = DateTime.Now
            };
        }

        // If installer says "Installing" (not explicitly failed), run verification
        if (result.Status == InstallStatus.Installing)
        {
            ReportProgress(new InstallProgress
            {
                CurrentAppId = app.Id,
                CurrentAppName = app.DisplayName,
                CurrentStatus = InstallStatus.Verifying,
                CurrentIndex = index + 1,
                TotalCount = total,
                StatusMessage = $"Verifying {app.DisplayName}..."
            });

            _logService.Info($"Verifying {app.DisplayName}...", app.Id);
            result = await _verificationService.VerifyAsync(app, result);

            if (!string.IsNullOrWhiteSpace(result.VerificationDetail))
                _logService.Debug(result.VerificationDetail, app.Id);
        }

        return result;
    }

    private void ReportProgress(InstallProgress progress)
        => ProgressChanged?.Invoke(this, progress);
}
