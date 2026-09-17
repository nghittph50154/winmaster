using System.Net.NetworkInformation;

namespace WinMaster.Services;

/// <summary>
/// Checks internet connectivity before attempting downloads.
/// </summary>
public class NetworkService
{
    private static readonly string[] PingHosts = ["8.8.8.8", "1.1.1.1", "dns.google"];

    /// <summary>
    /// Returns true if internet is reachable.
    /// Tries multiple hosts to reduce false negatives.
    /// </summary>
    public async Task<bool> IsInternetAvailableAsync()
    {
        foreach (var host in PingHosts)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeout: 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch
            {
                // Try next host
            }
        }
        return false;
    }
}
