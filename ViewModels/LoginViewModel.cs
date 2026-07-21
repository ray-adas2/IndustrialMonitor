using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Services;
using System.Windows;

namespace IndustrialMonitor.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
        => OnPropertyChanged(nameof(HasError));

    public bool LoginSuccess { get; private set; }

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private void Login(Window? window)
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "请输入用户名";
            return;
        }

        // 密码从 LoginWindow 的 PasswordBox 获取，通过参数传入
        // 实际绑定在 LoginWindow.xaml.cs 里处理
    }

    /// <summary>由 LoginWindow 调用，传入 PasswordBox 的值</summary>
    public bool TryLogin(string password)
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "请输入用户名";
            return false;
        }
        if (string.IsNullOrEmpty(password))
        {
            ErrorMessage = "请输入密码";
            return false;
        }
        if (_authService.Login(Username, password))
        {
            LoginSuccess = true;
            return true;
        }
        ErrorMessage = "用户名或密码错误，或账户已被禁用";
        return false;
    }
}
