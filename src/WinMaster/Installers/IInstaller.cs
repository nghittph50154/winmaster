using WinMaster.Models;

namespace WinMaster.Installers;

/// <summary>
/// Contract for all installer implementations.
/// Each installer type (winget, direct, fixed, etc.) implements this interface.
/// </summary>
public interface IInstaller
{
    /// <summary>
    /// Performs the installation of the given application.
    /// Reports output lines via <paramref name="outputProgress"/>.
    /// </summary>
    Task<InstallResult> InstallAsync(
        ApplicationEntry app,
        string? customInstallPath = null,
        bool isInteractive = false,
        IProgress<string>? outputProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if this installer can handle the given application entry.
    /// </summary>
    bool CanHandle(ApplicationEntry app);
}
