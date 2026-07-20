using IndustrialMonitor.ViewModels;
using System.Collections.ObjectModel;

namespace IndustrialMonitor.Services;

/// <summary>
/// 设备生命周期管理服务 — 全局单例，跨页面共享设备数据
/// </summary>
public class DeviceManagerService
{
    private readonly Func<DeviceViewModel> _deviceFactory;
    private int _deviceCounter = 1;

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    public DeviceViewModel? SelectedDevice { get; set; }

    public int TotalCount => Devices.Count;
    public int OnlineCount => Devices.Count(d => d.IsRunning);
    public int AlarmCount => Devices.Count(d => d.IsAlarming);

    public event Action? DeviceListChanged;

    public DeviceManagerService(Func<DeviceViewModel> deviceFactory)
    {
        _deviceFactory = deviceFactory;
    }

    public DeviceViewModel AddDevice()
    {
        var device = _deviceFactory();
        device.DeviceId = $"DEV-{_deviceCounter++:D3}";

        device.OnAlarmSaved = () => DeviceListChanged?.Invoke();

        Devices.Add(device);
        SelectedDevice = device;
        DeviceListChanged?.Invoke();
        return device;
    }

    public void RemoveDevice(DeviceViewModel? device)
    {
        if (device is null) return;

        device.StopCommand.Execute(null);
        Devices.Remove(device);

        if (SelectedDevice == device)
            SelectedDevice = Devices.FirstOrDefault();

        DeviceListChanged?.Invoke();
    }
}
