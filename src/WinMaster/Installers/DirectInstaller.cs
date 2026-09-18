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

        // Convert Google Drive share link to direct download URL
        url = ConvertGoogleDriveUrl(url);

        // Determine filename from URL or app id
        string fileName;
        if (url.Contains("drive.google.com") || url.Contains("docs.google.com"))
            fileName = $"{app.Id}-installer.exe";  // Drive links don't have filename in URL
        else
        {
            var uri = new Uri(url);
            fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"{app.Id}-installer{Path.GetExtension(uri.LocalPath)}";
        }

        var downloadPath = FileSystemHelper.GetTempDownloadPath(fileName);
        outputProgress?.Report($"Downloading {app.DisplayName}...");

        try
        {
            await DownloadFileAsync(url, downloadPath, cancellationToken);
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

    /// <summary>
    /// Converts a Google Drive share/view link to a direct download link.
    /// Supports: /file/d/ID/view, /open?id=ID, /uc?id=ID formats.
    /// </summary>
    private static string ConvertGoogleDriveUrl(string url)
    {
        if (!url.Contains("drive.google.com") && !url.Contains("docs.google.com"))
            return url;

        // Extract file ID
        string? fileId = null;

        // Format: /file/d/FILE_ID/view
        var match = System.Text.RegularExpressions.Regex.Match(url, @"/file/d/([a-zA-Z0-9_-]+)");
        if (match.Success) fileId = match.Groups[1].Value;

        // Format: id=FILE_ID
        if (fileId is null)
        {
            match = System.Text.RegularExpressions.Regex.Match(url, @"[?&]id=([a-zA-Z0-9_-]+)");
            if (match.Success) fileId = match.Groups[1].Value;
        }

        if (fileId is null) return url; // Can't parse, return as-is

        return $"https://drive.google.com/uc?export=download&id={fileId}&confirm=t";
    }

    /// <summary>
    /// Downloads a file, handling Google Drive large-file confirmation if needed.
    /// </summary>
    private static async Task DownloadFileAsync(string url, string destPath, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        // Google Drive returns HTML for large-file confirmation — handle it
        if (contentType.Contains("text/html"))
        {
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Extract confirm token (e.g. confirm=t&uuid=...)
            var uuidMatch = System.Text.RegularExpressions.Regex.Match(html, @"uuid=([a-zA-Z0-9_-]+)");
            var baseUrl = System.Text.RegularExpressions.Regex.Match(url, @"(https://drive\.google\.com/uc\?[^""]+)").Value;

            if (uuidMatch.Success && baseUrl.Length > 0)
            {
                var confirmUrl = $"{baseUrl}&uuid={uuidMatch.Groups[1].Value}";
                using var confirmResp = await HttpClient.GetAsync(confirmUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                confirmResp.EnsureSuccessStatusCode();
                await using var fs2 = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                await confirmResp.Content.CopyToAsync(fs2, cancellationToken);
                return;
            }
        }

        await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write);
        await response.Content.CopyToAsync(fileStream, cancellationToken);
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
