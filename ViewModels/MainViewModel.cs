using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.Views;
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

        private readonly AuthService _authService;

        public string CurrentUserName => _authService.CurrentUser?.Username ?? "";
        public string CurrentUserRole => _authService.CurrentUser?.Role ?? "";
        public bool IsViewer => _authService.CurrentUser?.Role == "Viewer";
        public bool CanModify => _authService.CurrentUser?.Role is "Admin" or "Operator";

        public MainViewModel(IAlarmLogService alarmLogService,
                             IServiceProvider serviceProvider,
                             DeviceManagerService deviceManager,
                             AuthService authService)
        {
            _alarmLogService = alarmLogService;
            _serviceProvider = serviceProvider;
            _deviceManager = deviceManager;
            _authService = authService;

            // 按角色过滤菜单
            Navigation.ApplyRoleFilter(authService);

            // 首次启动时若没有已保存的设备，自动创建一个
            if (_deviceManager.TotalCount == 0)
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
                "Users"      => _serviceProvider.GetRequiredService<UserManagementViewModel>(),
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

        [RelayCommand]
        private void Logout()
        {
            _authService.Logout();

            // 防止主窗口关闭时自动关机
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 关闭主窗口
            foreach (Window w in Application.Current.Windows)
                if (w is not Views.LoginWindow)
                    w.Close();

            // 重新弹出登录
            var loginWindow = App.ServiceProvider.GetRequiredService<Views.LoginWindow>();
            if (loginWindow.ShowDialog() == true)
            {
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
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
