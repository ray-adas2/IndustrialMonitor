using IndustrialMonitor.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace IndustrialMonitor.Services;

public class DeviceManagerService
{
    private readonly Func<DeviceViewModel> _deviceFactory;
    private readonly string _savePath;
    private int _deviceCounter = 1;

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();
    public DeviceViewModel? SelectedDevice { get; set; }

    public int TotalCount => Devices.Count;
    public int OnlineCount => Devices.Count(d => d.IsRunning);
    public int RunningCount => Devices.Count(d => d.IsRunning);
    public int AlarmCount => Devices.Count(d => d.IsAlarming);

    public event Action? DeviceListChanged;

    public DeviceManagerService(Func<DeviceViewModel> deviceFactory)
    {
        _deviceFactory = deviceFactory;
        _savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.json");
        LoadDevices();
    }

    public DeviceViewModel AddDevice()
    {
        var device = _deviceFactory();
        device.DeviceId = $"DEV-{_deviceCounter++:D3}";
        device.OnAlarmSaved = () => DeviceListChanged?.Invoke();

        Devices.Add(device);
        SelectedDevice = device;
        SaveDevices();
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

        SaveDevices();
        DeviceListChanged?.Invoke();
    }

    private void SaveDevices()
    {
        try
        {
            var ids = Devices.Select(d => d.DeviceId).ToList();
            var json = JsonSerializer.Serialize(new { DeviceIds = ids, Counter = _deviceCounter });
            File.WriteAllText(_savePath, json);
        }
        catch { /* 忽略保存错误 */ }
    }

    private void LoadDevices()
    {
        try
        {
            if (!File.Exists(_savePath)) return;
            var json = File.ReadAllText(_savePath);
            var data = JsonSerializer.Deserialize<DeviceSaveData>(json);
            if (data is null) return;

            _deviceCounter = data.Counter;
            foreach (var id in data.DeviceIds)
            {
                var device = _deviceFactory();
                device.DeviceId = id;
                device.OnAlarmSaved = () => DeviceListChanged?.Invoke();
                Devices.Add(device);
            }
            if (Devices.Count > 0)
                SelectedDevice = Devices[0];
        }
        catch { /* 忽略加载错误 */ }
    }

    private class DeviceSaveData
    {
        public List<string> DeviceIds { get; set; } = new();
        public int Counter { get; set; } = 1;
    }
}
