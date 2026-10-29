using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinMaster.ViewModels;

/// <summary>
/// Root ViewModel managing the 3 main menus and overall app state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public InstallViewModel InstallVM { get; }
    public TweaksViewModel TweaksVM { get; }
    public PlaceholderViewModel Menu3VM { get; }

    [ObservableProperty]
    private object _currentView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstallActive))]
    [NotifyPropertyChangedFor(nameof(IsMenu2Active))]
    [NotifyPropertyChangedFor(nameof(IsMenu3Active))]
    private string _activeMenu = "install";

    public bool IsInstallActive => ActiveMenu == "install";
    public bool IsMenu2Active => ActiveMenu == "menu2";
    public bool IsMenu3Active => ActiveMenu == "menu3";

    public string AppVersion => "v1.1.7";

    public MainViewModel(InstallViewModel installVM, TweaksViewModel tweaksVM)
    {
        InstallVM = installVM;
        TweaksVM = tweaksVM;

        Menu3VM = new PlaceholderViewModel(
            "WinPE",
            "Tạo USB Boot WinPE cứu hộ máy tính, tích hợp bộ công cụ phân vùng ổ đĩa, sao lưu / phục hồi hệ thống và cài driver cứu hộ chuyên dụng.\n\n" +
            "Tính năng này đang được phát triển và sẽ ra mắt chính thức trong phiên bản v1.2.",
            "v1.2");

        _currentView = InstallVM;
    }

    [RelayCommand]
    private void NavigateTo(string menu)
    {
        ActiveMenu = menu;
        CurrentView = menu switch
        {
            "menu2" => TweaksVM,
            "menu3" => Menu3VM,
            _ => InstallVM
        };
    }
}
