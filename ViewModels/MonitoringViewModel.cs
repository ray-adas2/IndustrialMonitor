using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace IndustrialMonitor.ViewModels;

public partial class MonitoringViewModel : ObservableObject
{
    private readonly IAlarmLogService _alarmLogService;
    private readonly DeviceManagerService _deviceManager;

    public ObservableCollection<DeviceViewModel> Devices => _deviceManager.Devices;

    [ObservableProperty]
    private DeviceViewModel? _selectedDevice;

    public ObservableCollection<AlarmRecord> AlarmHistory { get; } = new();

    public MonitoringViewModel(DeviceManagerService deviceManager, IAlarmLogService alarmLogService)
    {
        _deviceManager = deviceManager;
        _alarmLogService = alarmLogService;
        SelectedDevice = Devices.FirstOrDefault();
        _ = LoadAlarmHistoryAsync();
        _deviceManager.DeviceListChanged += () => _ = LoadAlarmHistoryAsync();
    }

    [RelayCommand]
    private void AddDevice() => _deviceManager.AddDevice();

    [RelayCommand]
    private void RemoveDevice(DeviceViewModel? device) => _deviceManager.RemoveDevice(device);

    private async Task LoadAlarmHistoryAsync()
    {
        var records = await _alarmLogService.GetAllAsync();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            AlarmHistory.Clear();
            foreach (var r in records) AlarmHistory.Add(r);
        });
    }
}
