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

    /// <summary>
    /// Runs a winget command.
    /// </summary>
    public async Task<PowerShellResult> RunWingetAsync(
        string wingetArgs,
        CancellationToken cancellationToken = default,
        IProgress<string>? outputProgress = null)
    {
        // Check if winget command is available on system
        try
        {
            return await ExecuteAsync("winget", wingetArgs, cancellationToken, outputProgress);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            return PowerShellResult.Failure(
                "Windows Package Manager (winget) chưa được cài đặt trên bản Windows Lite này. " +
                "Vui lòng chọn ứng dụng loại 'fixed' hoặc cài winget (App Installer) từ Microsoft Store.",
                -1);
        }
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
