using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using WinMaster.Infrastructure;
using WinMaster.Models;

namespace WinMaster.Installers;

/// <summary>
/// Installs applications by downloading directly from an official URL or Google Drive,
/// handles zip auto-extraction, and executes the installer.
/// </summary>
public class DirectInstaller : IInstaller
{
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

        string downloadedFilePath;
        try
        {
            downloadedFilePath = await DownloadFileAsync(url, app, customInstallPath, outputProgress, cancellationToken);
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

        // Determine file category and run installation/extraction
        var category = FileSystemHelper.GetFileCategory(downloadedFilePath);
        result = await RunInstallerFile(downloadedFilePath, category, app, result, customInstallPath, isInteractive, outputProgress, cancellationToken);

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.CompletedAt = DateTime.Now;
        return result;
    }

    /// <summary>
    /// Converts a Google Drive share/view link to an export download link.
    /// Supports: /file/d/ID/view, /open?id=ID, /uc?id=ID formats.
    /// </summary>
    private static string ConvertGoogleDriveUrl(string url)
    {
        if (!url.Contains("drive.google.com") && !url.Contains("docs.google.com"))
            return url;

        string? fileId = null;

        var match = Regex.Match(url, @"/file/d/([a-zA-Z0-9_-]+)");
        if (match.Success) fileId = match.Groups[1].Value;

        if (fileId is null)
        {
            match = Regex.Match(url, @"[?&]id=([a-zA-Z0-9_-]+)");
            if (match.Success) fileId = match.Groups[1].Value;
        }

        if (fileId is null) return url;

        return $"https://drive.google.com/uc?export=download&id={fileId}";
    }

    /// <summary>
    /// Downloads a file from direct URL or Google Drive (handling large-file confirmation).
    /// Returns the full path to the downloaded file.
    /// </summary>
    private static async Task<string> DownloadFileAsync(
        string url,
        ApplicationEntry app,
        string? customInstallPath,
        IProgress<string>? outputProgress,
        CancellationToken cancellationToken)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromHours(1)
        };
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        outputProgress?.Report($"Connecting to download source for {app.DisplayName}...");
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        HttpResponseMessage fileResponse = response;
        string? resolvedFileName = null;

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (mediaType.Contains("text/html"))
        {
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Extract file name from warning page: <span class="uc-name-size"><a ...>Filename</a>
            var nameMatch = Regex.Match(html, @"<span class=""uc-name-size""><a [^>]+>([^<]+)</a>");
            if (nameMatch.Success)
                resolvedFileName = nameMatch.Groups[1].Value.Trim();

            // Extract form action and hidden inputs
            var formMatch = Regex.Match(html, @"<form[^>]+id=""download-form""[^>]+action=""([^""]+)""");
            if (!formMatch.Success)
                formMatch = Regex.Match(html, @"<form[^>]+action=""([^""]+)""");

            if (formMatch.Success)
            {
                var action = formMatch.Groups[1].Value;
                var inputs = Regex.Matches(html, @"<input[^>]+type=""hidden""[^>]+name=""([^""]+)""[^>]+value=""([^""]*)""");
                var query = new List<string>();
                foreach (Match m in inputs)
                {
                    query.Add($"{Uri.EscapeDataString(m.Groups[1].Value)}={Uri.EscapeDataString(m.Groups[2].Value)}");
                }
                var confirmUrl = action + "?" + string.Join("&", query);
                outputProgress?.Report($"Confirming download for {resolvedFileName ?? app.DisplayName}...");
                fileResponse = await client.GetAsync(confirmUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                fileResponse.EnsureSuccessStatusCode();
            }
        }

        // Determine real filename from headers if available
        if (fileResponse.Content.Headers.ContentDisposition?.FileName != null)
        {
            resolvedFileName = fileResponse.Content.Headers.ContentDisposition.FileName.Trim('"', ' ');
        }

        if (string.IsNullOrWhiteSpace(resolvedFileName))
        {
            if (url.Contains("drive.google.com") || url.Contains("docs.google.com"))
            {
                resolvedFileName = $"{app.Id}-installer.exe";
            }
            else
            {
                var uri = new Uri(url);
                resolvedFileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(resolvedFileName))
                    resolvedFileName = $"{app.Id}-installer.exe";
            }
        }

        var targetFolder = !string.IsNullOrWhiteSpace(customInstallPath) && Directory.Exists(customInstallPath)
            ? customInstallPath
            : FileSystemHelper.GetDownloadsFolder();

        var downloadPath = Path.Combine(targetFolder, resolvedFileName);

        var totalBytes = fileResponse.Content.Headers.ContentLength;
        var totalMB = totalBytes.HasValue ? totalBytes.Value / (1024.0 * 1024.0) : 0;

        outputProgress?.Report($"Downloading {resolvedFileName} ({(totalMB > 0 ? $"{totalMB:F1} MB" : "size unknown")})...");

        await using (var contentStream = await fileResponse.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            var lastReport = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if ((DateTime.UtcNow - lastReport).TotalMilliseconds >= 1500)
                {
                    lastReport = DateTime.UtcNow;
                    var readMB = totalRead / (1024.0 * 1024.0);
                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        var percent = (int)(totalRead * 100 / totalBytes.Value);
                        outputProgress?.Report($"Downloading {resolvedFileName}: {readMB:F1} MB / {totalMB:F1} MB ({percent}%)...");
                    }
                    else
                    {
                        outputProgress?.Report($"Downloading {resolvedFileName}: {readMB:F1} MB...");
                    }
                }
            }
        }

        FileSystemHelper.UnblockFile(downloadPath);
        outputProgress?.Report($"Download complete: {resolvedFileName}");
        return downloadPath;
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
        var targetFolder = !string.IsNullOrWhiteSpace(customInstallPath) && Directory.Exists(customInstallPath)
            ? customInstallPath
            : FileSystemHelper.GetDownloadsFolder();

        if (category == FileCategory.Archive)
        {
            // If it is a ZIP archive, extract it and check for an installer executable
            if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var extractDir = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(filePath));
                    outputProgress?.Report($"Extracting archive to: {extractDir}...");

                    if (Directory.Exists(extractDir))
                        Directory.Delete(extractDir, true);

                    ZipFile.ExtractToDirectory(filePath, extractDir, overwriteFiles: true);

                    var exeFiles = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);

                    // 1. Look for genuine setup / installer executable
                    var setupExe = exeFiles.FirstOrDefault(f =>
                    {
                        var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                        return name is "setup" or "installer" or "install"
                            || name.EndsWith("_setup") || name.EndsWith("-setup")
                            || name.EndsWith("_install") || name.EndsWith("-installer");
                    });

                    if (setupExe != null)
                    {
                        outputProgress?.Report($"Found installer: {Path.GetFileName(setupExe)}. Starting installation...");
                        FileSystemHelper.UnblockFile(setupExe);
                        return await RunExecutableProcess(setupExe, app, result, isInteractive, outputProgress, cancellationToken);
                    }

                    // 2. Look for main application executable (portable app like CapCut)
                    var mainAppExe = exeFiles.FirstOrDefault(f =>
                    {
                        var name = Path.GetFileNameWithoutExtension(f);
                        return name.Equals("CapCut", StringComparison.OrdinalIgnoreCase)
                            || name.Equals(app.DisplayName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)
                            || name.Equals(app.Id.Replace("-", ""), StringComparison.OrdinalIgnoreCase);
                    });

                    if (mainAppExe != null)
                    {
                        outputProgress?.Report($"Portable application ready: {Path.GetFileName(mainAppExe)}");
                        FileSystemHelper.UnblockFile(mainAppExe);

                        // Create Desktop shortcut
                        try
                        {
                            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                            var shortcutPath = Path.Combine(desktop, $"{app.DisplayName}.lnk");
                            FileSystemHelper.CreateShortcut(shortcutPath, mainAppExe);
                            outputProgress?.Report($"Created Desktop shortcut: {app.DisplayName}");
                        }
                        catch { }

                        result.Status = InstallStatus.Success;
                        result.VerificationDetail = $"Extracted to {extractDir} (Main app: {Path.GetFileName(mainAppExe)})";
                        return result;
                    }

                    // 3. Extracted successfully without runnable setup
                    outputProgress?.Report($"Archive extracted to: {extractDir}");
                    result.Status = InstallStatus.Success;
                    result.VerificationDetail = $"Extracted to: {extractDir}";
                    return result;
                }
                catch (Exception ex)
                {
                    outputProgress?.Report($"Auto-extract note: {ex.Message}. Archive kept at: {filePath}");
                }
            }

            outputProgress?.Report($"Archive saved to: {filePath}");
            outputProgress?.Report($"Please extract and install manually if needed.");
            result.Status = InstallStatus.Success;
            result.VerificationDetail = $"Archive saved to: {filePath}";
            return result;
        }

        // Executable (.exe) or MSI (.msi)
        return await RunExecutableProcess(filePath, app, result, isInteractive, outputProgress, cancellationToken);
    }

    private static async Task<InstallResult> RunExecutableProcess(
        string filePath,
        ApplicationEntry app,
        InstallResult result,
        bool isInteractive,
        IProgress<string>? outputProgress,
        CancellationToken cancellationToken)
    {
        FileSystemHelper.UnblockFile(filePath);
        var category = FileSystemHelper.GetFileCategory(filePath);
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
            var workingDir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(workingDir) || !Directory.Exists(workingDir))
                workingDir = AppDomain.CurrentDomain.BaseDirectory;

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDir,
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

            // Exit code 0 or 3010 (restart required) indicate success
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
