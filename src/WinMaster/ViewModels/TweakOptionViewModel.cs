using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace WinMaster.ViewModels;

public partial class TweakOptionViewModel : ObservableObject
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }

    [ObservableProperty]
    private string _commandText;

    public string ShellType { get; } // "powershell" or "cmd"
    public bool ShowCode { get; }

    [ObservableProperty]
    private bool _isSelected;

    // Toggle switch properties (dành cho mục có hỗ trợ chuyển đổi Online / Offline như IDM)
    public bool HasToggleSwitch => Id == "idm_act";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleModeText))]
    [NotifyPropertyChangedFor(nameof(ToggleModeToolTip))]
    private bool _isOnlineMode = true; // Mặc định Online

    public string ToggleModeText => IsOnlineMode ? "ONLINE" : "OFFLINE";
    public string ToggleModeToolTip => IsOnlineMode
        ? "Chế độ Online: Chạy script kích hoạt trực tiếp từ server Coporton qua Internet."
        : "Chế độ Offline: Chạy bộ script và file kích hoạt từ thư mục nội bộ trong máy.";

    private readonly string _onlineCommand;
    private readonly string _offlineCommand;

    public TweakOptionViewModel(string id, string title, string description, string commandText, string shellType = "powershell", bool showCode = true)
    {
        Id = id;
        Title = title;
        Description = description;
        _commandText = commandText;
        _onlineCommand = commandText;
        _offlineCommand = @"& "".\scripts\IDM\ActivateIDM.ps1""";
        ShellType = shellType;
        ShowCode = showCode;
    }

    partial void OnIsOnlineModeChanged(bool value)
    {
        if (Id == "idm_act")
        {
            CommandText = value ? _onlineCommand : _offlineCommand;
        }
    }

    [RelayCommand]
    private void Copy()
    {
        if (!string.IsNullOrEmpty(CommandText))
        {
            Clipboard.SetText(CommandText);
        }
    }
}