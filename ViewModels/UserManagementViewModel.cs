using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;

namespace IndustrialMonitor.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty] private string _newUsername = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _newRole = "Viewer";
    [ObservableProperty] private string _statusMessage = string.Empty;

    public List<string> Roles { get; } = new() { "Operator", "Viewer" };

    public UserManagementViewModel(AuthService authService)
    {
        _authService = authService;
        LoadUsers();
    }

    [RelayCommand]
    private void AddUser()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
        {
            StatusMessage = "用户名和密码不能为空";
            return;
        }
        try
        {
            _authService.AddUser(NewUsername, NewPassword, NewRole);
            StatusMessage = $"用户 {NewUsername} 创建成功";
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            LoadUsers();
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleUser(User? user)
    {
        if (user is null) return;
        // 管理员账户不可被禁用
        if (user.Role == "Admin")
        {
            StatusMessage = "管理员账户不可被禁用";
            return;
        }
        // 不能禁用自己
        if (user.Id == _authService.CurrentUser?.Id)
        {
            StatusMessage = "不能禁用当前登录用户";
            return;
        }
        _authService.SetUserActive(user.Id, !user.IsActive);
        LoadUsers();
    }

    [RelayCommand]
    private void DeleteUser(User? user)
    {
        if (user is null) return;
        if (user.Role == "Admin")
        {
            StatusMessage = "管理员账户不可删除";
            return;
        }
        if (user.Id == _authService.CurrentUser?.Id)
        {
            StatusMessage = "不能删除当前登录用户";
            return;
        }
        try
        {
            _authService.DeleteUser(user.Id, user.Role);
            StatusMessage = $"用户 {user.Username} 已删除";
            LoadUsers();
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Refresh() => LoadUsers();

    private void LoadUsers()
    {
        var users = _authService.GetAllUsers();
        Users.Clear();
        foreach (var u in users) Users.Add(u);
    }
}
