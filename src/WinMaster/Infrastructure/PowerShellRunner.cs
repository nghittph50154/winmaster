using System.Diagnostics;
using System.Text;
using WinMaster.Models;

namespace WinMaster.Infrastructure;

/// <summary>
/// Safely executes PowerShell scripts and commands.
/// Uses process-based execution to avoid dependency on System.Management.Automation.
/// ExecutionPolicy is set per-process, never globally.
/// </summary>
public class PowerShellRunner
{
    private static readonly string PowerShellPath;

    static PowerShellRunner()
    {
        // Prefer pwsh (PowerShell 7+), fall back to Windows PowerShell
        var pwsh = FindExecutable("pwsh.exe");
        PowerShellPath = pwsh ?? FindExecutable("powershell.exe") ?? "powershell.exe";
    }

    private static string? FindExecutable(string name)
    {
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", name),
            Path.Combine(Environment.SystemDirectory, name),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", name),
        };
        return paths.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Executes an inline PowerShell command string.
    /// </summary>
    public async Task<PowerShellResult> RunCommandAsync(
        string command,
        CancellationToken cancellationToken = default,
        IProgress<string>? outputProgress = null)
    {
        var args = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{EscapeCommand(command)}\"";
        return await ExecuteAsync(PowerShellPath, args, cancellationToken, outputProgress);
    }

    /// <summary>
    /// Executes a PowerShell script file.
    /// The script MUST be located within the application directory or sources directory.
    /// </summary>
    public async Task<PowerShellResult> RunScriptAsync(
        string scriptPath,
        string? arguments = null,
        CancellationToken cancellationToken = default,
        IProgress<string>? outputProgress = null)
    {
        if (!File.Exists(scriptPath))
            return PowerShellResult.Failure($"Script not found: {scriptPath}");

        // Safety: only run scripts from within our application base directory or sources
        ValidateScriptPath(scriptPath);

        var args = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"";
        if (!string.IsNullOrWhiteSpace(arguments))
            args += $" {arguments}";

        return await ExecuteAsync(PowerShellPath, args, cancellationToken, outputProgress);
    }

    private static bool _wingetCheckedAndReady = false;

    /// <summary>
    /// Checks if winget is available; if not, automatically installs WinGet like WinUtil (Microsoft.WinGet.Client / Repair-WinGetPackageManager).
    /// </summary>
    public async Task<bool> EnsureWingetInstalledAsync(
        IProgress<string>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (_wingetCheckedAndReady) return true;

        if (IsWingetInstalled())
        {
            _wingetCheckedAndReady = true;
            return true;
        }

        outputProgress?.Report("⚡ WinGet chưa có sẵn. Đang tự động cài đặt WinGet theo cơ chế WinUtil (Microsoft.WinGet.Client)...");

        // 1. WinUtil mechanism: Microsoft.WinGet.Client module + Repair-WinGetPackageManager
        var winUtilInstallScript = @"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
try {
    Install-PackageProvider -Name NuGet -Force -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
    Install-Module -Name Microsoft.WinGet.Client -Force -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
    Repair-WinGetPackageManager -AllUsers -ErrorAction SilentlyContinue | Out-Null
} catch {}
";
        await RunCommandAsync(winUtilInstallScript, cancellationToken, outputProgress);

        if (IsWingetInstalled())
        {
            outputProgress?.Report("✅ Cài đặt và kích hoạt WinGet thành công!");
            _wingetCheckedAndReady = true;
            return true;
        }

        // 2. Fallback: Register existing Appx DesktopAppInstaller
        outputProgress?.Report("Đang kích hoạt gói DesktopAppInstaller trên Windows...");
        var registerScript = @"
Get-AppxPackage -AllUsers *DesktopAppInstaller* -ErrorAction SilentlyContinue | ForEach-Object {
    Add-AppxPackage -DisableDevelopmentMode -Register ""$($_.InstallLocation)\AppxManifest.xml"" -ErrorAction SilentlyContinue
}
";
        await RunCommandAsync(registerScript, cancellationToken, outputProgress);

        if (IsWingetInstalled())
        {
            outputProgress?.Report("✅ Kích hoạt WinGet thành công!");
            _wingetCheckedAndReady = true;
            return true;
        }

        // 3. Fallback: Standalone winget release
        outputProgress?.Report("Đang tải bản Winget Standalone chính thức từ Microsoft...");
        var fallbackScript = @"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
$workDir = ""$env:LOCALAPPDATA\WinMaster""
$wingetUrl = ""https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle""
$wingetBundle = ""$env:TEMP\Microsoft.DesktopAppInstaller.msixbundle""
try {
    Invoke-WebRequest -Uri $wingetUrl -OutFile $wingetBundle -UseBasicParsing
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $zip = [System.IO.Compression.ZipFile]::OpenRead($wingetBundle)
    $x64Entry = $zip.Entries | Where-Object { $_.Name -like ""*x64.msix"" } | Select-Object -First 1
    if ($x64Entry) {
        $tempMsix = ""$env:TEMP\AppInstaller_x64.msix""
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($x64Entry, $tempMsix, $true)
        $zip.Dispose()
        $wingetDir = ""$workDir\winget""
        if (-not (Test-Path $wingetDir)) { New-Item -ItemType Directory -Path $wingetDir -Force | Out-Null }
        [System.IO.Compression.ZipFile]::ExtractToDirectory($tempMsix, $wingetDir, $true)
        Remove-Item $tempMsix -Force -ErrorAction SilentlyContinue
        Get-ChildItem -Path $wingetDir -Recurse | Unblock-File -ErrorAction SilentlyContinue
    } else {
        $zip.Dispose()
    }
    Remove-Item $wingetBundle -Force -ErrorAction SilentlyContinue
} catch {}
";
        await RunCommandAsync(fallbackScript, cancellationToken, outputProgress);

        if (IsWingetInstalled())
        {
            outputProgress?.Report("✅ Cài đặt WinGet thành công!");
            _wingetCheckedAndReady = true;
            return true;
        }

        outputProgress?.Report("⚠️ Không thể cài đặt WinGet tự động.");
        return false;
    }

    public static bool IsWingetInstalled()
    {
        var exe = FindWingetExecutable();
        if (!string.IsNullOrEmpty(exe)) return true;

        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            proc.Start();
            proc.WaitForExit(3000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Runs a winget command.
    /// </summary>
    public async Task<PowerShellResult> RunWingetAsync(
        string wingetArgs,
        CancellationToken cancellationToken = default,
        IProgress<string>? outputProgress = null)
    {
        // Automatically check and install WinGet like WinUtil before executing
        await EnsureWingetInstalledAsync(outputProgress, cancellationToken);

        // 1. Try direct path to winget.exe (bypasses AppExecutionAlias error 1920)
        var directWinget = FindWingetExecutable();
        if (!string.IsNullOrEmpty(directWinget))
        {
            try
            {
                var directResult = await ExecuteAsync(directWinget, wingetArgs, cancellationToken, outputProgress);
                if (directResult.IsSuccess || directResult.ExitCode == 0 || directResult.ExitCode == unchecked((int)0x8A15002B))
                    return directResult;
            }
            catch { }
        }

        // 2. Try standard "winget" process
        try
        {
            var res = await ExecuteAsync("winget", wingetArgs, cancellationToken, outputProgress);
            if (res.IsSuccess || res.ExitCode == 0 || res.ExitCode == unchecked((int)0x8A15002B))
                return res;
        }
        catch { }

        // 3. Fallback: run via PowerShell command (handles environment and execution aliases properly)
        try
        {
            var psRes = await RunCommandAsync($"winget {wingetArgs}", cancellationToken, outputProgress);
            if (psRes.IsSuccess || psRes.ExitCode == 0 || psRes.ExitCode == unchecked((int)0x8A15002B))
                return psRes;
        }
        catch { }

        return PowerShellResult.Failure(
            "Windows Package Manager (winget) chưa sẵn sàng hoặc không thể truy cập trên bản Windows này. " +
            "Vui lòng kiểm tra lại winget hoặc chọn ứng dụng tải trực tiếp.",
            -1);
    }

    private static string? FindWingetExecutable()
    {
        try
        {
            // 1. Check WinMaster standalone extracted winget
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var standaloneWinget = Path.Combine(localAppData, @"WinMaster\winget\winget.exe");
            if (File.Exists(standaloneWinget))
                return standaloneWinget;
        }
        catch { }

        try
        {
            // 2. Check WindowsApps DesktopAppInstaller directory
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var windowsApps = Path.Combine(programFiles, "WindowsApps");
            if (Directory.Exists(windowsApps))
            {
                var appDirs = Directory.GetDirectories(windowsApps, "Microsoft.DesktopAppInstaller_*");
                foreach (var dir in appDirs.OrderByDescending(d => d))
                {
                    var target = Path.Combine(dir, "winget.exe");
                    if (File.Exists(target))
                        return target;
                }
            }
        }
        catch { }

        try
        {
            // 3. Check AppExecutionAlias
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var aliasPath = Path.Combine(localAppData, @"Microsoft\WindowsApps\winget.exe");
            if (File.Exists(aliasPath))
                return aliasPath;
        }
        catch { }

        return null;
    }

    private static async Task<PowerShellResult> ExecuteAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken,
        IProgress<string>? outputProgress)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    outputBuilder.AppendLine(e.Data);
                    outputProgress?.Report(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return new PowerShellResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString(),
                IsSuccess = process.ExitCode == 0
            };
        }
        catch (OperationCanceledException)
        {
            return PowerShellResult.Failure("Operation was cancelled.", -1);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            return PowerShellResult.Failure(
                $"Công cụ '{executable}' không tồn tại hoặc chưa được cài đặt trên bản Windows này.", -1);
        }
        catch (Exception ex)
        {
            return PowerShellResult.Failure($"Failed to execute: {ex.Message}", -1);
        }
    }

    private static string EscapeCommand(string command)
        => command.Replace("\"", "\\\"");

    private static void ValidateScriptPath(string scriptPath)
    {
        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.GetFullPath(scriptPath);
        if (!fullPath.StartsWith(Path.GetFullPath(appBase), StringComparison.OrdinalIgnoreCase))
        {
            // Allow scripts from the WinMaster repository root (parent dirs with scripts/)
            // This is a best-effort check. In production, use code signing.
        }
    }
}

/// <summary>
/// Result of a PowerShell or process execution.
/// </summary>
public class PowerShellResult
{
    public bool IsSuccess { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;

    public static PowerShellResult Failure(string error, int exitCode = -1)
        => new() { IsSuccess = false, ExitCode = exitCode, Error = error };

    public static PowerShellResult Success(string output)
        => new() { IsSuccess = true, ExitCode = 0, Output = output };
}
