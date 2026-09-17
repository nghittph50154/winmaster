using System.Diagnostics;
using WinMaster.Infrastructure;
using WinMaster.Models;

namespace WinMaster.Installers;

/// <summary>
/// Handles installations from fixed source files in the sources/ directory.
/// 
/// Rules:
/// - Does NOT assume file extension (checks actual file)
/// - Does NOT download an alternate version
/// - Does NOT run archive files automatically (copies to Downloads/)
/// - Reports clearly when source file is missing
/// </summary>
public class FixedSourceInstaller : IInstaller
{
    public bool CanHandle(ApplicationEntry app)
        => app.Installer.InstallerTypeEnum == InstallerType.Fixed;

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

        // Resolve the source directory
        var sourcesRoot = FileSystemHelper.ResolveSourcesDirectory();
        if (sourcesRoot is null)
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = "Fixed sources directory not found. Please ensure the 'sources/' folder exists.";
            sw.Stop(); result.Duration = sw.Elapsed;
            return result;
        }

        // Resolve the specific source subdirectory from config
        var sourceDir = app.Installer.SourceDir;
        string searchDir;

        if (!string.IsNullOrWhiteSpace(sourceDir))
        {
            // If sourceDir is relative, resolve from sources root
            searchDir = Path.IsPathRooted(sourceDir)
                ? sourceDir
                : Path.Combine(sourcesRoot, sourceDir.Replace("sources/", "").Replace("sources\\", "").TrimStart('/').TrimStart('\\'));
        }
        else
        {
            searchDir = sourcesRoot;
        }

        // Find the actual file — check for expectedPattern first, then by app ID
        string? sourceFile = null;

        if (!string.IsNullOrWhiteSpace(app.Installer.ExpectedPattern))
        {
            sourceFile = FileSystemHelper.FindFileContaining(searchDir, app.Installer.ExpectedPattern);
        }

        // Fallback: search by app name
        if (sourceFile is null)
        {
            sourceFile = FileSystemHelper.FindFileContaining(searchDir, app.Id)
                      ?? FileSystemHelper.FindFileContaining(searchDir, app.Name);
        }

        if (sourceFile is null || !File.Exists(sourceFile))
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"Fixed source file not found in: {searchDir}\n" +
                                  $"Expected pattern: {app.Installer.ExpectedPattern ?? app.Id}\n" +
                                  $"Please provide the source file in the sources directory.";
            sw.Stop(); result.Duration = sw.Elapsed;
            return result;
        }

        outputProgress?.Report($"Found source file: {Path.GetFileName(sourceFile)}");

        // Check actual file type — do NOT assume
        var category = FileSystemHelper.GetFileCategory(sourceFile);
        outputProgress?.Report($"File type detected: {category}");

        if (category == FileCategory.Archive)
        {
            // Archive: copy to target directory (custom or Downloads), do NOT auto-run
            var targetFolder = !string.IsNullOrWhiteSpace(customInstallPath) && Directory.Exists(customInstallPath)
                ? customInstallPath
                : FileSystemHelper.GetDownloadsFolder();

            var destPath = Path.Combine(targetFolder, Path.GetFileName(sourceFile));

            outputProgress?.Report($"Copying archive to target folder: {targetFolder}...");
            File.Copy(sourceFile, destPath, overwrite: true);

            outputProgress?.Report($"Download completed.");
            outputProgress?.Report($"File saved to: {destPath}");
            outputProgress?.Report($"Please extract and install manually.");

            result.Status = InstallStatus.Success;
            result.VerificationDetail = $"Archive saved to: {destPath}";
            sw.Stop(); result.Duration = sw.Elapsed;
            result.CompletedAt = DateTime.Now;
            return result;
        }

        if (category == FileCategory.Unknown)
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"Unknown file format: {Path.GetExtension(sourceFile)}. Cannot install automatically.";
            sw.Stop(); result.Duration = sw.Elapsed;
            return result;
        }

        // EXE or MSI — run with or without GUI window based on isInteractive
        outputProgress?.Report($"Running installer for {app.DisplayName}...");

        try
        {
            ProcessStartInfo psi;

            if (category == FileCategory.Installer)
            {
                // MSI
                psi = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = isInteractive ? $"/i \"{sourceFile}\"" : $"/i \"{sourceFile}\" /quiet /norestart",
                    UseShellExecute = isInteractive,
                    CreateNoWindow = !isInteractive
                };
            }
            else
            {
                // EXE
                psi = new ProcessStartInfo
                {
                    FileName = sourceFile,
                    Arguments = isInteractive ? "" : "/S",
                    UseShellExecute = isInteractive,
                    CreateNoWindow = !isInteractive
                };
            }

            using var process = Process.Start(psi);
            if (process is null)
            {
                result.Status = InstallStatus.Failed;
                result.ErrorMessage = "Failed to start installer.";
                sw.Stop(); result.Duration = sw.Elapsed;
                return result;
            }

            await process.WaitForExitAsync(cancellationToken);
            result.ExitCode = process.ExitCode;

            result.Status = (process.ExitCode == 0 || process.ExitCode == 3010)
                ? InstallStatus.Installing
                : InstallStatus.Failed;

            if (process.ExitCode == 3010)
                outputProgress?.Report("Installation complete. A system restart may be required.");
            else if (result.Status == InstallStatus.Failed)
                result.ErrorMessage = $"Installer exited with code {process.ExitCode}";
        }
        catch (Exception ex)
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"Error running installer: {ex.Message}";
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.CompletedAt = DateTime.Now;
        return result;
    }
}
