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
        => app.Installer.InstallerTypeEnum is InstallerType.Winget or InstallerType.Store;

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

        var wingetId = !string.IsNullOrWhiteSpace(app.Installer.WingetId)
            ? app.Installer.WingetId
            : app.Installer.StoreId;

        if (string.IsNullOrWhiteSpace(wingetId))
        {
            result.Status = InstallStatus.Failed;
            result.ErrorMessage = "No package ID or Store ID configured.";
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        // Check if package is from msstore
        var isMsStore = string.Equals(app.Installer.Source, "msstore", StringComparison.OrdinalIgnoreCase)
                     || app.Installer.InstallerTypeEnum == InstallerType.Store
                     || wingetId.StartsWith("msstore:", StringComparison.OrdinalIgnoreCase);

        if (wingetId.StartsWith("msstore:", StringComparison.OrdinalIgnoreCase))
        {
            wingetId = wingetId.Substring("msstore:".Length);
        }

        var sourceArg = isMsStore ? "--source msstore" : "";
        var targetDir = !string.IsNullOrWhiteSpace(customInstallPath)
            ? customInstallPath
            : FileSystemHelper.GetDownloadsFolder();

        // 1. Download only mode (Chỉ tải file không cài đặt)
        if (isDownloadOnly)
        {
            outputProgress?.Report($"Đang tải gói cài đặt {app.DisplayName} về: {targetDir}...");
            try { Directory.CreateDirectory(targetDir); } catch { }

            var startTime = DateTime.Now;
            var downloadArgs = $"download --id \"{wingetId}\" {sourceArg} -d \"{targetDir}\" --accept-package-agreements --accept-source-agreements";
            var psResult = await _psRunner.RunWingetAsync(downloadArgs, cancellationToken, outputProgress);

            // Tự động xóa sạch các file .yaml manifest do winget download sinh ra
            CleanupManifestYamlFiles(targetDir, startTime);

            sw.Stop();
            result.Duration = sw.Elapsed;
            result.ExitCode = psResult.ExitCode;
            result.CompletedAt = DateTime.Now;

            if (psResult.IsSuccess || psResult.ExitCode == 0)
            {
                result.Status = InstallStatus.Success;
                outputProgress?.Report($"✅ Đã tải file cài đặt {app.DisplayName} vào: {targetDir}");
            }
            else
            {
                result.Status = InstallStatus.Failed;
                result.ErrorMessage = string.IsNullOrWhiteSpace(psResult.Error)
                    ? $"Winget download returned exit code {psResult.ExitCode}"
                    : psResult.Error.Trim();
            }
            return result;
        }

        // 2. Regular Install mode
        outputProgress?.Report($"Downloading and installing {app.DisplayName} via package manager...");

        var modeArgs = isInteractive ? "--interactive" : "--silent --disable-interactivity";
        var wingetArgs = $"install --id \"{wingetId}\" {sourceArg} {modeArgs} --accept-package-agreements --accept-source-agreements";
        if (!string.IsNullOrWhiteSpace(customInstallPath) && !isMsStore)
        {
            wingetArgs += $" --location \"{customInstallPath}\"";
            outputProgress?.Report($"Target install location: {customInstallPath}");
        }
        var psInstallResult = await _psRunner.RunWingetAsync(wingetArgs, cancellationToken, outputProgress);

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.ExitCode = psInstallResult.ExitCode;
        result.CompletedAt = DateTime.Now;

        if (psInstallResult.IsSuccess)
        {
            result.Status = InstallStatus.Installing; // will be updated by verification
        }
        else
        {
            // winget exit code 0x8A15002B = already installed (treat as success)
            if (psInstallResult.ExitCode == unchecked((int)0x8A15002B))
            {
                result.Status = InstallStatus.Installing;
                outputProgress?.Report($"{app.DisplayName} was already installed or is up to date.");
            }
            else
            {
                result.Status = InstallStatus.Failed;
                result.ErrorMessage = string.IsNullOrWhiteSpace(psInstallResult.Error)
                    ? $"Package manager returned exit code {psInstallResult.ExitCode}"
                    : psInstallResult.Error.Trim();
            }
        }

        return result;
    }

    /// <summary>
    /// Xóa các file .yaml manifest do Microsoft Winget tự động xuất ra khi tải gói,
    /// giữ cho thư mục tải về chỉ còn file cài đặt (.exe / .msi) sạch sẽ.
    /// </summary>
    private static void CleanupManifestYamlFiles(string targetDir, DateTime startTime)
    {
        try
        {
            if (!Directory.Exists(targetDir)) return;

            var threshold = startTime.AddSeconds(-10);
            var patterns = new[] { "*.yaml", "*.yml" };

            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(targetDir, pattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime >= threshold || info.CreationTime >= threshold ||
                            info.Name.Contains("_inno_") || info.Name.Contains("_exe_") ||
                            info.Name.Contains("_User_") || info.Name.Contains("_Machine_"))
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // File có thể đang bị tiến trình khác giữ khóa
                    }
                }
            }
        }
        catch
        {
        }
    }
}
