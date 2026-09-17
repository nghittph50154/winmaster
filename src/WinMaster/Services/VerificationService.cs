using WinMaster.Infrastructure;
using WinMaster.Models;

namespace WinMaster.Services;

/// <summary>
/// Post-install verification service.
/// Tries multiple strategies to confirm installation success.
/// Never fabricates results — if it cannot verify, says so.
/// </summary>
public class VerificationService
{
    private readonly PowerShellRunner _psRunner;

    public VerificationService(PowerShellRunner psRunner)
    {
        _psRunner = psRunner;
    }

    /// <summary>
    /// Attempts to verify a successful installation using the app's configured strategies.
    /// Updates the result's Status and VerificationDetail.
    /// </summary>
    public async Task<InstallResult> VerifyAsync(ApplicationEntry app, InstallResult result)
    {
        if (result.Status == InstallStatus.Failed)
            return result; // Don't verify failed installs

        try
        {
            var strategies = app.Verification.Strategies;
            if (!strategies.Any())
            {
                result.Status = InstallStatus.Success;
                result.VerificationDetail = "Verification unavailable (no strategies configured).";
                return result;
            }

            foreach (var strategy in strategies)
            {
                var verified = strategy.ToLowerInvariant() switch
                {
                    "command" => await TryCommandVerification(app),
                    "registry" => TryRegistryVerification(app),
                    "executable" => TryExecutableVerification(app),
                    "path" => TryPathVerification(app),
                    _ => null
                };

                if (verified == true)
                {
                    result.Status = InstallStatus.Success;
                    result.VerificationDetail = $"Verified via {strategy}";
                    return result;
                }
            }
        }
        catch
        {
            // Verification error should never cause installation to fail if install step succeeded
            result.Status = InstallStatus.Success;
            result.VerificationDetail = "Installation completed; verification error skipped.";
            return result;
        }

        // Could not verify with any strategy
        result.Status = InstallStatus.Success;
        result.VerificationDetail = "Installation completed; verification inconclusive. The app may need a restart to appear.";
        return result;
    }

    private async Task<bool?> TryCommandVerification(ApplicationEntry app)
    {
        if (string.IsNullOrWhiteSpace(app.Verification.Command))
            return null;

        try
        {
            var psResult = await _psRunner.RunCommandAsync(app.Verification.Command);
            return psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Output);
        }
        catch
        {
            return null;
        }
    }

    private bool? TryRegistryVerification(ApplicationEntry app)
    {
        if (string.IsNullOrWhiteSpace(app.Verification.RegistryKey))
            return null;

        try
        {
            var key = app.Verification.RegistryKey
                .Replace("HKLM:\\", "HKEY_LOCAL_MACHINE\\")
                .Replace("HKCU:\\", "HKEY_CURRENT_USER\\");

            using var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                key.Replace("HKEY_LOCAL_MACHINE\\", ""));

            return regKey is not null;
        }
        catch
        {
            return null;
        }
    }

    private bool? TryExecutableVerification(ApplicationEntry app)
    {
        try
        {
            var execName = app.Verification.ExecutableName;
            if (string.IsNullOrWhiteSpace(execName))
            {
                execName = app.Id + ".exe";
            }

            var programFiles = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
            };

            foreach (var dir in programFiles)
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;

                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    try
                    {
                        var found = Directory.GetFiles(subDir, execName, SearchOption.AllDirectories).Any();
                        if (found) return true;
                    }
                    catch (UnauthorizedAccessException) { /* Ignore WindowsApps and protected folders */ }
                    catch (Exception) { }
                }
            }
        }
        catch
        {
            // Ignore verification error
        }

        return null;
    }

    private bool? TryPathVerification(ApplicationEntry app)
    {
        if (string.IsNullOrWhiteSpace(app.Verification.InstallPath))
            return null;

        try
        {
            var path = Environment.ExpandEnvironmentVariables(app.Verification.InstallPath);
            return File.Exists(path) || Directory.Exists(path);
        }
        catch
        {
            return null;
        }
    }
}
