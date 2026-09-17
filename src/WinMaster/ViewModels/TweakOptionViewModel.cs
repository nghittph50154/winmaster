using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace WinMaster.ViewModels;

public partial class TweakOptionViewModel : ObservableObject
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public string CommandText { get; }
    public string ShellType { get; } // "powershell" or "cmd"

    [ObservableProperty]
    private bool _isSelected;

    public TweakOptionViewModel(string id, string title, string description, string commandText, string shellType = "powershell")
    {
        Id = id;
        Title = title;
        Description = description;
        CommandText = commandText;
        ShellType = shellType;
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

