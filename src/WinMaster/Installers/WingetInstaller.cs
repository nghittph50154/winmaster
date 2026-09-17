using System.Diagnostics;
using WinMaster.Infrastructure;
using WinMaster.Models;

namespace WinMaster.Installers;

/// <summary>
/// Installs applications using the Windows Package Manager (winget).
/// Winget must be available on the system (Windows 10 1809+ / App Installer).
/// </summary>
public class WingetInstaller : IInstaller
{
    private readonly PowerShellRunner _psRunner;

    public WingetInstaller(PowerShellRunner psRunner)
    {
        _psRunner = psRunner;
    }

    public bool CanHandle(ApplicationEntry app)
        => app.Installer.InstallerTypeEnum == InstallerType.Winget;

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

        var wingetId = app.Installer.WingetId;
        if (string.IsNullOrWhiteSpace(wingetId))
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = "No winget package ID configured.";
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        outputProgress?.Report($"Downloading and installing {app.DisplayName} via package manager...");

        var modeArgs = isInteractive ? "--interactive" : "--silent --disable-interactivity";
        var wingetArgs = $"install --id \"{wingetId}\" {modeArgs} --accept-package-agreements --accept-source-agreements";
        if (!string.IsNullOrWhiteSpace(customInstallPath))
        {
            wingetArgs += $" --location \"{customInstallPath}\"";
            outputProgress?.Report($"Target install location: {customInstallPath}");
        }
        var psResult = await _psRunner.RunWingetAsync(wingetArgs, cancellationToken, outputProgress);

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.ExitCode = psResult.ExitCode;
        result.CompletedAt = DateTime.Now;

        if (psResult.IsSuccess)
        {
            result.Status = InstallStatus.Installing; // will be updated by verification
        }
        else
        {
            // winget exit code 0x8A15002B = already installed (treat as success)
            if (psResult.ExitCode == unchecked((int)0x8A15002B))
            {
                result.Status = InstallStatus.Installing;
                outputProgress?.Report($"{app.DisplayName} was already installed or is up to date.");
            }
            else
            {
                result.Status = InstallStatus.Failed;
                result.ErrorMessage = string.IsNullOrWhiteSpace(psResult.Error)
                    ? $"Package manager returned exit code {psResult.ExitCode}"
                    : psResult.Error.Trim();
            }
        }

        return result;
    }
}
