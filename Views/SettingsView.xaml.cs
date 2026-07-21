using IndustrialMonitor.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace IndustrialMonitor.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.OldPassword = OldPwdBox.Password;
            vm.NewPassword = NewPwdBox.Password;
            vm.ConfirmPassword = ConfirmPwdBox.Password;
            vm.ChangePasswordCommand.Execute(null);
            OldPwdBox.Password = string.Empty;
            NewPwdBox.Password = string.Empty;
            ConfirmPwdBox.Password = string.Empty;
        }
    }
}
