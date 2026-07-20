using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly DeviceManagerService _deviceManager;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty] private int _totalDevices;
    [ObservableProperty] private int _onlineDevices;
    [ObservableProperty] private int _alarmCount;
    [ObservableProperty] private string _uptime = "—";

    public DashboardViewModel(DeviceManagerService deviceManager)
    {
        _deviceManager = deviceManager;
        RefreshStats();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (s, e) => RefreshStats();
        _refreshTimer.Start();
    }

    private void RefreshStats()
    {
        TotalDevices = _deviceManager.TotalCount;
        OnlineDevices = _deviceManager.OnlineCount;
        AlarmCount = _deviceManager.AlarmCount;
    }
}
