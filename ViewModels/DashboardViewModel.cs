using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialMonitor.Services;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly DeviceManagerService _deviceManager;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty] private int _totalDevices;
    [ObservableProperty] private int _onlineDevices;
    [ObservableProperty] private int _alarmCount;
    [ObservableProperty] private int _runningCount;
    [ObservableProperty] private double _onlinePercent;
    [ObservableProperty] private double _alarmPercent;

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
        RunningCount = _deviceManager.RunningCount;
        AlarmCount = _deviceManager.AlarmCount;
        OnlinePercent = TotalDevices > 0 ? (double)OnlineDevices / TotalDevices * 100 : 0;
        AlarmPercent = TotalDevices > 0 ? (double)AlarmCount / TotalDevices * 100 : 0;
    }
}
