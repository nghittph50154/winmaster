using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinMaster.ViewModels;

/// <summary>
/// Placeholder ViewModel for Menu 2 and Menu 3.
/// Designed to be replaced with real functionality in future versions.
/// </summary>
public partial class PlaceholderViewModel : ObservableObject
{
    public string MenuTitle { get; }
    public string ComingSoonDescription { get; }
    public string PlannedVersion { get; }

    public PlaceholderViewModel(string menuTitle, string comingSoonDescription, string plannedVersion = "v2.0")
    {
        MenuTitle = menuTitle;
        ComingSoonDescription = comingSoonDescription;
        PlannedVersion = plannedVersion;
    }
}
