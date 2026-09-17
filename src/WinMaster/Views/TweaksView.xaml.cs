using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinMaster.ViewModels;

namespace WinMaster.Views;

public partial class TweaksView : UserControl
{
    public TweaksView()
    {
        InitializeComponent();
    }

    private void OptionCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TweakOptionViewModel optionVM)
        {
            if (DataContext is TweaksViewModel mainVM)
            {
                mainVM.SelectOption(optionVM);
            }
        }
    }
}
