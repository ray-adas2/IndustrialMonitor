using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using System.Collections.ObjectModel;

namespace IndustrialMonitor.ViewModels;

public partial class NavigationViewModel : ObservableObject
{
    [ObservableProperty]
    private NavMenuItem? _selectedMenuItem;

    public ObservableCollection<NavMenuItem> MenuItems { get; } = new()
    {
        new() { Icon = "\U0001F4CA", Title = "首页总览", Key = "Dashboard" },
        new() { Icon = "\U0001F5A5", Title = "设备管理", Key = "Devices" },
        new() { Icon = "\U0001F4C8", Title = "实时监控", Key = "Monitoring" },
        new() { Icon = "⚠",  Title = "报警中心", Key = "Alarms" },
        new() { Icon = "\U0001F4DC", Title = "历史记录", Key = "History" },
        new() { Icon = "⚙",  Title = "系统设置", Key = "Settings" },
    };
}
