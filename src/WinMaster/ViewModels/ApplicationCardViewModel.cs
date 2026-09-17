using CommunityToolkit.Mvvm.ComponentModel;
using WinMaster.Models;

namespace WinMaster.ViewModels;

/// <summary>
/// ViewModel wrapping a single ApplicationEntry for display in the UI.
/// Drives ApplicationCard binding.
/// </summary>
public partial class ApplicationCardViewModel : ObservableObject
{
    private readonly ApplicationEntry _entry;

    public ApplicationCardViewModel(ApplicationEntry entry)
    {
        _entry = entry;
    }

    // --- Exposed data properties ---
    public string Id => _entry.Id;
    public string DisplayName => _entry.DisplayName;
    public string Description => _entry.Description;
    public string Category => _entry.Category;
    public bool IsPending => _entry.IsPending;
    public bool RequiresAdmin => _entry.RequiresAdmin;
    public string IconKey => _entry.Id; // Used to look up icon resource

    /// <summary>Returns an emoji icon for the app — used as fallback icon in cards.</summary>
    public string IconEmoji => _entry.Id switch
    {
        "microsoft-edge"    => "🌐",
        "google-chrome"     => "🟡",
        "brave"             => "🦁",
        "tor-browser"       => "🧅",
        "discord"           => "💬",
        "zoom"              => "📹",
        "telegram"          => "✈",
        "zalo"              => "💬",
        "revoltg"           => "⚡",
        "chatgpt-desktop"   => "🤖",
        "codex"             => "🧠",
        "cursor"            => "🖱",
        "git"               => "🔀",
        "github-desktop"    => "🐙",
        "nodejs"            => "🟢",
        "python3"           => "🐍",
        "vscode"            => "💙",
        "antigravity"       => "🚀",
        "notepadplusplus"   => "📝",
        "office365"         => "📊",
        "foxit-reader"      => "📄",
        "goodnotes"         => "📓",
        "steam"             => "🎮",
        "genshin-impact"    => "⚔",
        "roblox"            => "🎲",
        "xmcl"              => "⛏",
        "geforce-now"       => "🎯",
        "riot-client"       => "⚔",
        "autoruns"          => "🔍",
        "7zip"              => "🗜",
        "anydesk"           => "🖥",
        "cloudflare-warp"   => "🛡",
        "idm"               => "⬇",
        "rufus"             => "💾",
        "winrar"            => "📦",
        "tailscale"         => "🔒",
        "xdm"               => "⬇",
        "bcuninstaller"     => "🗑",
        "memreduct"         => "🧹",
        "windirstat"        => "📊",
        "windhawk"          => "🦅",
        "nilesoft-shell"    => "🐚",
        "fxsound"           => "🎵",
        "ventoy"            => "💿",
        "deskin"            => "🖥",
        "ultraviewer"       => "👁",
        "evkey"             => "⌨",
        "minitool-partition"=> "🔧",
        "obs-studio"        => "🔴",
        "vmware"            => "🖥",
        "xp-pen"            => "✏",
        "capcut"            => "🎬",
        "capcut-1.5"        => "🎬",
        "capcut-7.0"        => "🎬",
        "capcut-7.7"        => "🎬",
        "yindiao-g17-drive" => "⌨",
        "inphic-drive"      => "🖱",
        "aula-f87-drive"    => "⌨",
        "python-3128"       => "🐍",
        _                   => "📦"
    };

    /// <summary>Resolves downloaded icon image path if present, otherwise returns null.</summary>
    public string? IconPath
    {
        get
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "Icons", $"{_entry.Id}.png"),
                Path.Combine(baseDir, "Assets", "Icons", $"{_entry.Id}.ico"),
                Path.Combine(baseDir, "Assets", "Icons", $"{_entry.Id}.jpg"),
                Path.Combine(baseDir, "Assets", "Icons", $"{_entry.Id}.svg"),
                Path.Combine(baseDir, "Assets", "Icons", $"{_entry.Id}.webp")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }

    public bool HasImageIcon => !string.IsNullOrEmpty(IconPath);

    /// <summary>Returns the raw ApplicationEntry for the installer engine.</summary>
    public ApplicationEntry Entry => _entry;

    // --- UI State ---

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private InstallStatus _installStatus = InstallStatus.Pending;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    // Computed status display
    public bool IsInstalling => InstallStatus == InstallStatus.Installing || InstallStatus == InstallStatus.Downloading;
    public bool IsSuccess => InstallStatus == InstallStatus.Success;
    public bool IsError => InstallStatus == InstallStatus.Failed;
    public bool IsIdle => InstallStatus == InstallStatus.Pending;

    partial void OnInstallStatusChanged(InstallStatus value)
    {
        OnPropertyChanged(nameof(IsInstalling));
        OnPropertyChanged(nameof(IsSuccess));
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(IsIdle));
    }

    public void ResetStatus()
    {
        InstallStatus = InstallStatus.Pending;
        StatusMessage = string.Empty;
    }
}
