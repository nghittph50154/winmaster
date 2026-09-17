using System.Diagnostics;
using WinMaster.Infrastructure;
using WinMaster.Models;

namespace WinMaster.Installers;

/// <summary>
/// Installs applications by running a PowerShell script from the scripts/ directory.
/// Used for apps that require custom installation logic.
/// </summary>
public class PowerShellInstaller : IInstaller
{
    private readonly PowerShellRunner _psRunner;

    public PowerShellInstaller(PowerShellRunner psRunner)
    {
        _psRunner = psRunner;
    }

    public bool CanHandle(ApplicationEntry app)
        => app.Installer.InstallerTypeEnum == InstallerType.PowerShell;

    public async Task<InstallResult> InstallAsync(
        ApplicationEntry app,
        string? customInstallPath = null,
        bool isInteractive = false,
        IProgress<string>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new InstallResult
        {
            ApplicationId = app.Id,
            ApplicationName = app.DisplayName
        };

        var scriptName = app.Installer.PowerShellScript;
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = "No PowerShell script configured for this application.";
            sw.Stop(); result.Duration = sw.Elapsed;
            return result;
        }

        // Resolve script path from scripts/applications/
        var scriptPath = ResolveScriptPath(scriptName);
        if (scriptPath is null || !File.Exists(scriptPath))
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"PowerShell script not found: {scriptName}";
            sw.Stop(); result.Duration = sw.Elapsed;
            return result;
        }

        outputProgress?.Report($"Running installation script for {app.DisplayName}...");

        var psResult = await _psRunner.RunScriptAsync(scriptPath, null, cancellationToken, outputProgress);

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.ExitCode = psResult.ExitCode;
        result.CompletedAt = DateTime.Now;

        if (psResult.IsSuccess)
        {
            result.Status = InstallStatus.Installing;
        }
        else
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = string.IsNullOrWhiteSpace(psResult.Error)
                ? $"Script exited with code {psResult.ExitCode}"
                : psResult.Error.Trim();
        }

        return result;
    }

    private static string? ResolveScriptPath(string scriptName)
    {
        var appBase = AppDomain.CurrentDomain.BaseDirectory;

        // Try scripts/applications/ relative to exe
        var candidate = Path.Combine(appBase, "scripts", "applications", scriptName);
        if (File.Exists(candidate)) return candidate;

        // Walk up tree (development mode)
        var dir = new DirectoryInfo(appBase);
        while (dir is not null)
        {
            var sub = Path.Combine(dir.FullName, "scripts", "applications", scriptName);
            if (File.Exists(sub)) return sub;
            dir = dir.Parent;
        }

        return null;
    }
}

/// <summary>
/// Handles applications that are not yet configured.
/// Shows a clear error instead of crashing.
/// </summary>
public class PendingInstaller : IInstaller
{
    public bool CanHandle(ApplicationEntry app)
        => app.Installer.InstallerTypeEnum == InstallerType.Pending;

    public Task<InstallResult> InstallAsync(
        ApplicationEntry app,
        string? customInstallPath = null,
        bool isInteractive = false,
        IProgress<string>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        outputProgress?.Report($"⚠ {app.DisplayName} is not yet configured for installation.");

        return Task.FromResult(new InstallResult
        {
            ApplicationId = app.Id,
            ApplicationName = app.DisplayName,
            Status = InstallStatus.Failed,
            ErrorMessage = $"{app.DisplayName} installer configuration is pending. " +
                           $"This application has not yet been configured in WinMaster. " +
                           $"Please check for updates.",
            CompletedAt = DateTime.Now
        });
    }
}
