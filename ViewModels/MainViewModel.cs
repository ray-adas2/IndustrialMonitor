using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace IndustrialMonitor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAlarmLogService _alarmLogService;
        private readonly Func<DeviceViewModel> _deviceFactory;
        private int _deviceCounter = 1;

        //多设备Tasb集合
        public ObservableCollection<DeviceViewModel> Devices { get; } = new();

        // 当前选择的设备 Tab
        [ObservableProperty]
        private DeviceViewModel? _selectedDevice;

        // 暴露给 UI 的全局历史报警列表
        public ObservableCollection<AlarmRecord> AlarmHistory { get; } = new();

        public MainViewModel(IAlarmLogService alarmLogService,Func<DeviceViewModel> deviceFactory)
        {
            _alarmLogService = alarmLogService;
            _deviceFactory = deviceFactory;

            // 启动时默认新增第一个设备
            AddDevice();

            // 异步加载历史报错记录
            _ = LoadAlarmHistoryAsync();
        }

        // 添加设备命令
        [RelayCommand]
        private void AddDevice()
        {
            // 利用 DI 容器传入的工厂生成全新的 DeviceViewModel
            var device = _deviceFactory();
            device.DeviceId = $"Dev-{_deviceCounter++:D3}";

            // 订阅该设备的报警保存事件，自动刷新全局历史记录表
            device.OnAlarmSaved = () =>
            {
                _ = LoadAlarmHistoryAsync();
            };

            Devices.Add(device);
            SelectedDevice = device;
        }


        [RelayCommand]
        private void RemoveDevice(DeviceViewModel? device)
        {
            if (device == null) return;

            // 移除前先停止定时器和数据源
            device.StopCommand.Execute(null);

            Devices.Remove(device);

            if (SelectedDevice == device && Devices.Count > 0)
            {
                SelectedDevice = Devices[0];
            }
        }


        //异步加载历史记录
        private async Task LoadAlarmHistoryAsync()
        {
            var records = await _alarmLogService.GetAllAsync();
            Application.Current?.Dispatcher.Invoke(() =>
            {
                AlarmHistory.Clear();
                foreach (var r in records)
                {
                    AlarmHistory.Add(r);
                }
            });
        }

    }
}
