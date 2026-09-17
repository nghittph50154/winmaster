using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinMaster.ViewModels;

/// <summary>
/// Root ViewModel managing the 3 main menus and overall app state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public InstallViewModel InstallVM { get; }
    public PlaceholderViewModel Menu2VM { get; }
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

    public string AppVersion => "v1.0.0";

    public MainViewModel(InstallViewModel installVM)
    {
        InstallVM = installVM;
        Menu2VM = new PlaceholderViewModel(
            "Tweaks & Optimization",
            "System tweaks, performance optimization, and Windows customization tools. " +
            "Configure Windows settings, disable telemetry, and optimize startup.\n\n" +
            "This feature is currently under development.",
            "v1.1");

        Menu3VM = new PlaceholderViewModel(
            "System Tools",
            "Advanced system management, driver tools, and maintenance utilities. " +
            "Manage startup programs, clean temp files, and monitor system health.\n\n" +
            "This feature is currently under development.",
            "v1.2");

        _currentView = InstallVM;
    }

    [RelayCommand]
    private void NavigateTo(string menu)
    {
        ActiveMenu = menu;
        CurrentView = menu switch
        {
            "menu2" => Menu2VM,
            "menu3" => Menu3VM,
            _ => InstallVM
        };
    }
}
