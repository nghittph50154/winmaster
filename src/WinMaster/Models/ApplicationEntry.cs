using System.Text.Json.Serialization;

namespace WinMaster.Models;

/// <summary>
/// Represents the type of installer to use for an application.
/// </summary>
public enum InstallerType
{
    Winget,
    Direct,
    Fixed,
    PowerShell,
    Store,
    Pending   // Not yet configured — shows clear error to user
}

/// <summary>
/// Defines the version policy for an application.
/// </summary>
public enum VersionPolicy
{
    Latest,
    Fixed
}

/// <summary>
/// Verification strategy for post-install checks.
/// </summary>
public enum VerificationStrategy
{
    Executable,
    Registry,
    Command,
    Path,
    None
}

/// <summary>
/// Installer configuration for an application (from JSON config).
/// </summary>
public class InstallerConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "pending";

    [JsonPropertyName("wingetId")]
    public string? WingetId { get; set; }

    [JsonPropertyName("directUrl")]
    public string? DirectUrl { get; set; }

    [JsonPropertyName("fallback")]
    public string? Fallback { get; set; }

    [JsonPropertyName("powerShellScript")]
    public string? PowerShellScript { get; set; }

    [JsonPropertyName("sourceDir")]
    public string? SourceDir { get; set; }

    [JsonPropertyName("expectedPattern")]
    public string? ExpectedPattern { get; set; }

    [JsonPropertyName("storeId")]
    public string? StoreId { get; set; }

    /// <summary>Resolved installer type enum.</summary>
    [JsonIgnore]
    public InstallerType InstallerTypeEnum => Type.ToLowerInvariant() switch
    {
        "winget" => InstallerType.Winget,
        "direct" => InstallerType.Direct,
        "fixed" => InstallerType.Fixed,
        "powershell" => InstallerType.PowerShell,
        "store" => InstallerType.Store,
        _ => InstallerType.Pending
    };
}

/// <summary>
/// Verification configuration for post-install checks.
/// </summary>
public class VerificationConfig
{
    [JsonPropertyName("strategies")]
    public List<string> Strategies { get; set; } = new();

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("registryKey")]
    public string? RegistryKey { get; set; }

    [JsonPropertyName("executableName")]
    public string? ExecutableName { get; set; }

    [JsonPropertyName("installPath")]
    public string? InstallPath { get; set; }
}

/// <summary>
/// Core model for an application entry loaded from applications.json.
/// This is the "raw" configuration model — not the UI model.
/// </summary>
public class ApplicationEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("installer")]
    public InstallerConfig Installer { get; set; } = new();

    [JsonPropertyName("verification")]
    public VerificationConfig Verification { get; set; } = new();

    [JsonPropertyName("requiresAdmin")]
    public bool RequiresAdmin { get; set; }

    [JsonPropertyName("versionPolicy")]
    public string VersionPolicyStr { get; set; } = "latest";

    [JsonPropertyName("iconSource")]
    public string IconSource { get; set; } = "embedded";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "stable";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonIgnore]
    public VersionPolicy VersionPolicyEnum => VersionPolicyStr.ToLowerInvariant() == "fixed"
        ? VersionPolicy.Fixed
        : VersionPolicy.Latest;

    [JsonIgnore]
    public bool IsPending => Installer.InstallerTypeEnum == InstallerType.Pending;
}

/// <summary>
/// Root of the applications.json configuration file.
/// </summary>
public class ApplicationsConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("remoteBaseUrl")]
    public string RemoteBaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("applications")]
    public List<ApplicationEntry> Applications { get; set; } = new();
}
