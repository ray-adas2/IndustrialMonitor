using IndustrialMonitor.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace IndustrialMonitor.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
    }

    private void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
        {
            vm.NewPassword = NewUserPassword.Password;
            vm.AddUserCommand.Execute(null);
            NewUserPassword.Password = string.Empty;
        }
    }
}
