using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinMaster.ViewModels;

namespace WinMaster.Views;

public partial class InstallView : UserControl
{
    public InstallView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Clicking on a card (not just the checkbox) toggles selection.
    /// </summary>
    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ApplicationCardViewModel vm)
        {
            vm.IsSelected = !vm.IsSelected;
        }
    }
}
