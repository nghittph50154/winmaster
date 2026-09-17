using System.Diagnostics;
using System.Net.Http;
using WinMaster.Infrastructure;
using WinMaster.Models;

namespace WinMaster.Installers;

/// <summary>
/// Installs applications by downloading directly from an official URL,
/// then running the downloaded EXE or MSI.
/// </summary>
public class DirectInstaller : IInstaller
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(30),
        DefaultRequestHeaders = { { "User-Agent", "WinMaster/1.0 (+https://github.com/winmaster)" } }
    };

    public bool CanHandle(ApplicationEntry app)
        => app.Installer.InstallerTypeEnum == InstallerType.Direct;

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

        var url = app.Installer.DirectUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = "No direct download URL configured.";
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        // Determine filename from URL
        var uri = new Uri(url);
        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"{app.Id}-installer{Path.GetExtension(uri.LocalPath)}";

        var downloadPath = FileSystemHelper.GetTempDownloadPath(fileName);
        outputProgress?.Report($"Downloading {app.DisplayName}...");

        try
        {
            // Download the file
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            outputProgress?.Report($"Download complete. Installing {app.DisplayName}...");
        }
        catch (HttpRequestException ex)
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"Download failed: {ex.Message}";
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }
        catch (Exception ex)
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"Unexpected error during download: {ex.Message}";
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        // Determine file type and handle accordingly
        var category = FileSystemHelper.GetFileCategory(downloadPath);
        result = await RunInstallerFile(downloadPath, category, app, result, customInstallPath, isInteractive, outputProgress, cancellationToken);

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.CompletedAt = DateTime.Now;
        return result;
    }

    internal static async Task<InstallResult> RunInstallerFile(
        string filePath,
        FileCategory category,
        ApplicationEntry app,
        InstallResult result,
        string? customInstallPath,
        bool isInteractive,
        IProgress<string>? outputProgress,
        CancellationToken cancellationToken)
    {
        if (category == FileCategory.Archive)
        {
            var targetFolder = !string.IsNullOrWhiteSpace(customInstallPath) && Directory.Exists(customInstallPath)
                ? customInstallPath
                : FileSystemHelper.GetDownloadsFolder();

            var destPath = Path.Combine(targetFolder, Path.GetFileName(filePath));
            if (filePath != destPath && File.Exists(filePath))
                File.Move(filePath, destPath, overwrite: true);

            outputProgress?.Report($"Download completed.");
            outputProgress?.Report($"File saved to: {destPath}");
            outputProgress?.Report($"Please extract and install manually.");

            result.Status = InstallStatus.Success;
            result.VerificationDetail = $"Archive downloaded to: {destPath}";
            return result;
        }

        // EXE or MSI — run with or without GUI window based on isInteractive
        var executable = category == FileCategory.Installer ? "msiexec.exe" : filePath;
        var arguments = "";

        if (!isInteractive)
        {
            arguments = category == FileCategory.Installer
                ? $"/i \"{filePath}\" /quiet /norestart"
                : "/S /silent /quiet /norestart";
        }
        else
        {
            arguments = category == FileCategory.Installer
                ? $"/i \"{filePath}\""
                : "";
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = isInteractive,
                CreateNoWindow = !isInteractive
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                result.Status = InstallStatus.Failed;
                result.ErrorMessage = "Failed to start installer process.";
                return result;
            }

            await process.WaitForExitAsync(cancellationToken);
            result.ExitCode = process.ExitCode;

            // Exit code 0 or 3010 (restart required) are success
            result.Status = (process.ExitCode == 0 || process.ExitCode == 3010)
                ? InstallStatus.Installing
                : InstallStatus.Failed;

            if (process.ExitCode == 3010)
                outputProgress?.Report("Installation complete. A restart may be required.");
            else if (result.Status == InstallStatus.Failed)
                result.ErrorMessage = $"Installer exited with code {process.ExitCode}";
        }
        catch (Exception ex)
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = $"Error running installer: {ex.Message}";
        }

        return result;
    }
}
