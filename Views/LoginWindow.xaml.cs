using IndustrialMonitor.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace IndustrialMonitor.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        UsernameBox.Focus();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        AttemptLogin();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            AttemptLogin();
    }

    private void AttemptLogin()
    {
        if (_viewModel.TryLogin(PasswordBox.Password))
        {
            DialogResult = true;
            Close();
        }
    }
}
