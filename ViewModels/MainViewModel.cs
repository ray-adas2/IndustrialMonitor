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
        private readonly IServiceProvider _serviceProvider;
        private readonly DeviceManagerService _deviceManager;
        private readonly Dictionary<string, object?> _pageCache = new();

        // ── 导航（Shell 层）──
        public NavigationViewModel Navigation { get; } = new();

        [ObservableProperty]
        private object? _currentPage;

        public string Breadcrumb => Navigation.SelectedMenuItem is null
            ? "首页"
            : $"首页 / {Navigation.SelectedMenuItem.Title}";

        // ── 委托给 DeviceManagerService ──
        public ObservableCollection<DeviceViewModel> Devices => _deviceManager.Devices;

        public DeviceViewModel? SelectedDevice
        {
            get => _deviceManager.SelectedDevice;
            set => _deviceManager.SelectedDevice = value;
        }

        public ObservableCollection<AlarmRecord> AlarmHistory { get; } = new();

        public MainViewModel(IAlarmLogService alarmLogService,
                             IServiceProvider serviceProvider,
                             DeviceManagerService deviceManager)
        {
            _alarmLogService = alarmLogService;
            _serviceProvider = serviceProvider;
            _deviceManager = deviceManager;

            // 启动时创建第一个设备
            _deviceManager.AddDevice();
            _ = LoadAlarmHistoryAsync();
            _deviceManager.DeviceListChanged += async () => await LoadAlarmHistoryAsync();

            Navigation.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Navigation.SelectedMenuItem))
                {
                    OnPropertyChanged(nameof(Breadcrumb));
                    NavigateTo(Navigation.SelectedMenuItem?.Key ?? "");
                }
            };

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

        public bool IsMonitoringVisible => CurrentPage is null;

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
}
