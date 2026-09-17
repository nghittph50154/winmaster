using System.Security.Principal;

namespace WinMaster.Services;

/// <summary>
/// Manages Administrator privilege checking and elevation requests.
/// </summary>
public class AdminService
{
    /// <summary>
    /// Returns true if the current process is running as Administrator.
    /// </summary>
    public bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Re-launches the current process with elevated privileges via UAC.
    /// The calling code should exit the current process after calling this.
    /// </summary>
    /// <returns>True if elevation was successfully requested.</returns>
    public bool RequestElevation()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath is null) return false;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"   // This triggers UAC prompt
            };

            System.Diagnostics.Process.Start(psi);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User denied UAC
            return false;
        }
    }
}
