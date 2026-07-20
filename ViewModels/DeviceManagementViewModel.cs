using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;

namespace IndustrialMonitor.ViewModels;

public partial class DeviceManagementViewModel : ObservableObject
{
    private readonly DeviceManagerService _deviceManager;

    public ObservableCollection<DeviceViewModel> Devices => _deviceManager.Devices;

    public DeviceManagementViewModel(DeviceManagerService deviceManager)
    {
        _deviceManager = deviceManager;
    }

    [RelayCommand]
    private void AddDevice() => _deviceManager.AddDevice();

    [RelayCommand]
    private void RemoveDevice(DeviceViewModel? device) => _deviceManager.RemoveDevice(device);
}
