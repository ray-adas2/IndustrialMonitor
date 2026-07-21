using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;

namespace IndustrialMonitor.ViewModels;

public partial class NavigationViewModel : ObservableObject
{
    private readonly List<NavMenuItem> _allItems = new()
    {
        new() { Icon = "\U0001F4CA", Title = "首页总览", Key = "Dashboard" },
        new() { Icon = "\U0001F5A5", Title = "设备管理", Key = "Devices" },
        new() { Icon = "\U0001F4C8", Title = "实时监控", Key = "Monitoring" },
        new() { Icon = "⚠",  Title = "报警中心", Key = "Alarms" },
        new() { Icon = "\U0001F4DC", Title = "历史记录", Key = "History" },
        new() { Icon = "⚙",  Title = "系统设置", Key = "Settings" },
        new() { Icon = "👥",  Title = "用户管理", Key = "Users" },
    };

    [ObservableProperty]
    private NavMenuItem? _selectedMenuItem;

    public ObservableCollection<NavMenuItem> MenuItems { get; } = new();

    public void ApplyRoleFilter(AuthService authService)
    {
        MenuItems.Clear();
        foreach (var item in _allItems)
        {
            if (authService.CanAccessPage(item.Key))
                MenuItems.Add(item);
        }
    }
}
