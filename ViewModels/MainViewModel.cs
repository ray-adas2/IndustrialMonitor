using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace IndustrialMonitor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAlarmLogService _alarmLogService;
        private readonly Func<DeviceViewModel> _deviceFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, object?> _pageCache = new();
        private int _deviceCounter = 1;

        // ── 导航（Shell 层）──
        public NavigationViewModel Navigation { get; } = new();

        [ObservableProperty]
        private object? _currentPage;

        // CurrentPage 为 null 时显示监控区
        public bool IsMonitoringVisible => CurrentPage is null;

        public string Breadcrumb => Navigation.SelectedMenuItem is null
            ? "首页"
            : $"首页 / {Navigation.SelectedMenuItem.Title}";

        // ── 设备管理 ──
        public ObservableCollection<DeviceViewModel> Devices { get; } = new();

        [ObservableProperty]
        private DeviceViewModel? _selectedDevice;

        // 全局历史报警列表
        public ObservableCollection<AlarmRecord> AlarmHistory { get; } = new();

        public MainViewModel(IAlarmLogService alarmLogService, Func<DeviceViewModel> deviceFactory,
                             IServiceProvider serviceProvider)
        {
            _alarmLogService = alarmLogService;
            _deviceFactory = deviceFactory;
            _serviceProvider = serviceProvider;

            AddDevice();
            _ = LoadAlarmHistoryAsync();

            // 监听菜单切换 → 换页面
            Navigation.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Navigation.SelectedMenuItem))
                {
                    OnPropertyChanged(nameof(Breadcrumb));
                    NavigateTo(Navigation.SelectedMenuItem?.Key ?? "");
                }
            };

            // 默认选中实时监控
            Navigation.SelectedMenuItem = Navigation.MenuItems.FirstOrDefault(m => m.Key == "Monitoring");
        }

        private void NavigateTo(string key)
        {
            if (_pageCache.TryGetValue(key, out var cached))
            {
                CurrentPage = cached;
                return;
            }

            object? page = key switch
            {
                "Dashboard" => _serviceProvider.GetRequiredService<DashboardViewModel>(),
                "Devices"    => _serviceProvider.GetRequiredService<DeviceManagementViewModel>(),
                "Monitoring" => _serviceProvider.GetRequiredService<MonitoringViewModel>(),
                "Alarms"     => _serviceProvider.GetRequiredService<AlarmCenterViewModel>(),
                "History"    => _serviceProvider.GetRequiredService<HistoryViewModel>(),
                "Settings"   => _serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => null
            };

            _pageCache[key] = page;
            CurrentPage = page;
        }

        partial void OnCurrentPageChanged(object? value)
        {
            OnPropertyChanged(nameof(IsMonitoringVisible));
        }

        [RelayCommand]
        private void AddDevice()
        {
            var device = _deviceFactory();
            device.DeviceId = $"DEV-{_deviceCounter++:D3}";
            device.OnAlarmSaved = () => { _ = LoadAlarmHistoryAsync(); };
            Devices.Add(device);
            SelectedDevice = device;
        }

        [RelayCommand]
        private void RemoveDevice(DeviceViewModel? device)
        {
            if (device == null) return;
            device.StopCommand.Execute(null);
            Devices.Remove(device);
            if (SelectedDevice == device && Devices.Count > 0)
                SelectedDevice = Devices[0];
        }

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
}
