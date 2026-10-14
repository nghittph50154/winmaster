using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
        bool isDownloadOnly = false,
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

        // If download only, finish without running installer
        if (isDownloadOnly)
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
            result.CompletedAt = DateTime.Now;
            result.Status = InstallStatus.Success;
            outputProgress?.Report($"✅ Đã tải xong file cài đặt {app.DisplayName} vào: {downloadedFilePath}");
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
        var handler = new SocketsHttpHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            ConnectCallback = async (context, ct) =>
            {
                IPAddress[] addresses;
                try
                {
                    addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
                }
                catch
                {
                    // Fallback DNS qua DoH (1.1.1.1 / Google DNS) nếu DNS nhà mạng VN chặn tên miền (như Steam)
                    addresses = await ResolveWithFallbackDnsAsync(context.DnsEndPoint.Host, ct);
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
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

        // 1. Determine real filename from headers if available
        if (!string.IsNullOrWhiteSpace(fileResponse.Content.Headers.ContentDisposition?.FileName))
        {
            resolvedFileName = fileResponse.Content.Headers.ContentDisposition.FileName.Trim('"', ' ');
        }
        else if (!string.IsNullOrWhiteSpace(fileResponse.Content.Headers.ContentDisposition?.FileNameStar))
        {
            resolvedFileName = fileResponse.Content.Headers.ContentDisposition.FileNameStar.Trim('"', ' ');
        }

        // 2. Try extracting from final redirected RequestUri (e.g. Zalo redirects zalo-pc -> ZaloSetup-26.10.11.exe)
        if (string.IsNullOrWhiteSpace(resolvedFileName) || string.IsNullOrWhiteSpace(Path.GetExtension(resolvedFileName)))
        {
            var finalUri = fileResponse.RequestMessage?.RequestUri;
            if (finalUri != null)
            {
                var candidate = Path.GetFileName(finalUri.LocalPath);
                if (!string.IsNullOrWhiteSpace(candidate) && !string.IsNullOrWhiteSpace(Path.GetExtension(candidate)))
                {
                    resolvedFileName = candidate;
                }
            }
        }

        // 3. Fallback to initial URL or app Id
        if (string.IsNullOrWhiteSpace(resolvedFileName))
        {
            if (url.Contains("drive.google.com") || url.Contains("docs.google.com"))
            {
                resolvedFileName = $"{app.Id}-installer.exe";
            }
            else
            {
                var uri = new Uri(url);
                var candidate = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(candidate) && !string.IsNullOrWhiteSpace(Path.GetExtension(candidate)))
                {
                    resolvedFileName = candidate;
                }
                else if (!string.IsNullOrWhiteSpace(candidate))
                {
                    resolvedFileName = $"{candidate}.exe";
                }
                else
                {
                    resolvedFileName = $"{app.Id}-installer.exe";
                }
            }
        }

        // Clean filename (URL decode and strip invalid filename characters)
        try
        {
            resolvedFileName = Uri.UnescapeDataString(resolvedFileName);
        }
        catch { }

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            resolvedFileName = resolvedFileName.Replace(c, '_');
        }

        // 4. Guarantee filename has an installer extension (.exe default)
        if (string.IsNullOrWhiteSpace(Path.GetExtension(resolvedFileName)))
        {
            resolvedFileName = $"{resolvedFileName}.exe";
        }

        var targetFolder = !string.IsNullOrWhiteSpace(customInstallPath) && Directory.Exists(customInstallPath)
            ? customInstallPath
            : FileSystemHelper.GetDownloadsFolder();

        // Clean up legacy broken extensionless file if it exists in target folder (e.g. old "zalo-pc" file)
        try
        {
            var oldNoExtPath = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(resolvedFileName));
            if (File.Exists(oldNoExtPath) && string.IsNullOrWhiteSpace(Path.GetExtension(oldNoExtPath)))
            {
                File.Delete(oldNoExtPath);
            }
            if (app.Id == "zalo")
            {
                var oldZaloPath = Path.Combine(targetFolder, "zalo-pc");
                if (File.Exists(oldZaloPath) && string.IsNullOrWhiteSpace(Path.GetExtension(oldZaloPath)))
                {
                    File.Delete(oldZaloPath);
                }
            }
            if (app.Id == "xmcl")
            {
                foreach (var oldDeskGap in Directory.GetFiles(targetFolder, "*deskgap*"))
                {
                    try { File.Delete(oldDeskGap); } catch { }
                }
                foreach (var oldZip in Directory.GetFiles(targetFolder, "xmcl*.zip"))
                {
                    try { File.Delete(oldZip); } catch { }
                }
            }
        }
        catch { }

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

        if (category == FileCategory.StorePackage || Path.GetExtension(filePath).Equals(".appinstaller", StringComparison.OrdinalIgnoreCase))
        {
            outputProgress?.Report($"Preparing Store / App Installer package: {Path.GetFileName(filePath)}...");
            try
            {
                var isAppInstaller = Path.GetExtension(filePath).Equals(".appinstaller", StringComparison.OrdinalIgnoreCase);

                if (!isInteractive)
                {
                    // Silent/non-interactive install via Add-AppxPackage
                    var psArgs = isAppInstaller
                        ? $"-NoProfile -NonInteractive -Command \"Add-AppxPackage -AppInstallerFile '{filePath}'\""
                        : $"-NoProfile -NonInteractive -Command \"Add-AppxPackage -Path '{filePath}'\"";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = psArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync(cancellationToken);
                        if (proc.ExitCode == 0)
                        {
                            result.Status = InstallStatus.Success;
                            result.VerificationDetail = $"Installed via AppInstaller: {Path.GetFileName(filePath)}";
                            outputProgress?.Report($"✅ Đã cài đặt xong gói: {Path.GetFileName(filePath)}");
                            return result;
                        }
                    }
                }

                // Interactive mode or fallback: open Windows App Installer UI directly
                outputProgress?.Report($"Opening Windows App Installer for: {Path.GetFileName(filePath)}...");
                var shellPsi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(shellPsi);
                }
                catch
                {
                    var fileUri = new Uri(filePath).AbsoluteUri;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"ms-appinstaller:?source={fileUri}",
                        UseShellExecute = true
                    });
                }

                result.Status = InstallStatus.Success;
                result.VerificationDetail = $"Launched App Installer for {Path.GetFileName(filePath)}";
                outputProgress?.Report($"✅ Đã mở App Installer cho {app.DisplayName}. Vui lòng xác nhận trên cửa sổ cài đặt Windows.");
                return result;
            }
            catch (Exception ex)
            {
                outputProgress?.Report($"AppInstaller error: {ex.Message}. File saved to: {filePath}");
                result.Status = InstallStatus.Success;
                result.VerificationDetail = $"Saved to {filePath}";
                return result;
            }
        }

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

                    // 2. Look for main application executable (portable app like CapCut, XMCL)
                    var mainAppExe = exeFiles.FirstOrDefault(f =>
                    {
                        var name = Path.GetFileNameWithoutExtension(f);
                        return name.Equals("CapCut", StringComparison.OrdinalIgnoreCase)
                            || name.Equals(app.DisplayName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)
                            || name.Equals(app.Id.Replace("-", ""), StringComparison.OrdinalIgnoreCase)
                            || (!string.IsNullOrWhiteSpace(app.Verification?.ExecutableName) && name.Equals(Path.GetFileNameWithoutExtension(app.Verification.ExecutableName), StringComparison.OrdinalIgnoreCase));
                    });

                    if (mainAppExe != null)
                    {
                        outputProgress?.Report($"Portable application ready: {Path.GetFileName(mainAppExe)}");
                        FileSystemHelper.UnblockFile(mainAppExe);

                        // Install portable application cleanly to %LocalAppData%\Programs\<AppDisplayName>
                        try
                        {
                            var programsBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
                            Directory.CreateDirectory(programsBase);
                            var programsDir = Path.Combine(programsBase, app.DisplayName);

                            if (Directory.Exists(programsDir))
                            {
                                try { Directory.Delete(programsDir, true); } catch { }
                            }

                            if (!Directory.Exists(programsDir))
                            {
                                Directory.Move(extractDir, programsDir);
                                var newMainApp = Directory.GetFiles(programsDir, Path.GetFileName(mainAppExe), SearchOption.AllDirectories).FirstOrDefault();
                                if (newMainApp != null)
                                {
                                    mainAppExe = newMainApp;
                                    extractDir = programsDir;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            outputProgress?.Report($"Note: Keeping app at {extractDir} ({ex.Message})");
                        }

                        // Unblock all executables in target dir
                        try
                        {
                            foreach (var exe in Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories))
                            {
                                FileSystemHelper.UnblockFile(exe);
                            }
                        }
                        catch { }

                        // Create Desktop shortcut
                        try
                        {
                            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                            var shortcutPath = Path.Combine(desktop, $"{app.DisplayName}.lnk");
                            FileSystemHelper.CreateShortcut(shortcutPath, mainAppExe);
                            outputProgress?.Report($"Created Desktop shortcut: {app.DisplayName}");
                        }
                        catch { }

                        // Create Start Menu shortcut
                        try
                        {
                            var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs");
                            if (Directory.Exists(startMenu))
                            {
                                var startShortcut = Path.Combine(startMenu, $"{app.DisplayName}.lnk");
                                FileSystemHelper.CreateShortcut(startShortcut, mainAppExe);
                            }
                        }
                        catch { }

                        if (isInteractive)
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = mainAppExe,
                                    WorkingDirectory = Path.GetDirectoryName(mainAppExe) ?? "",
                                    UseShellExecute = true
                                });
                            }
                            catch { }
                        }

                        result.Status = InstallStatus.Success;
                        result.VerificationDetail = $"Installed to {extractDir} ({Path.GetFileName(mainAppExe)})";
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
                : "/S";
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

    /// <summary>
    /// Phân giải tên miền qua Cloudflare DNS-over-HTTPS (1.1.1.1) khi DNS nhà mạng Việt Nam chặn (như Steam, Discord).
    /// </summary>
    private static async Task<IPAddress[]> ResolveWithFallbackDnsAsync(string host, CancellationToken ct)
    {
        // IP dự phòng trực tiếp cho Steam CDN nếu DNS hoàn toàn không phản hồi
        if (host.Contains("steamstatic.com"))
        {
            return new[]
            {
                IPAddress.Parse("23.49.104.176"),
                IPAddress.Parse("23.49.104.184"),
                IPAddress.Parse("23.210.7.177"),
                IPAddress.Parse("23.210.7.165")
            };
        }

        try
        {
            using var dohClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            dohClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/dns-json"));
            var json = await dohClient.GetStringAsync($"https://1.1.1.1/dns-query?name={Uri.EscapeDataString(host)}&type=A", ct);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Answer", out var answer) && answer.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var ips = new List<IPAddress>();
                foreach (var item in answer.EnumerateArray())
                {
                    if (item.TryGetProperty("data", out var dataProp) && IPAddress.TryParse(dataProp.GetString(), out var ip))
                    {
                        ips.Add(ip);
                    }
                }
                if (ips.Count > 0) return ips.ToArray();
            }
        }
        catch
        {
        }

        throw new SocketException((int)SocketError.HostNotFound);
    }
}
