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
    private readonly List<AlarmRecord> _allAlarms = new();
    private const int PAGE_SIZE = 10;
    private int _currentPage;

    public ObservableCollection<DeviceViewModel> Devices => _deviceManager.Devices;
    public ObservableCollection<AlarmRecord> PagedAlarms { get; } = new();

    [ObservableProperty] private DeviceViewModel? _selectedDevice;
    [ObservableProperty] private string _pageInfo = "第 0/0 页";

    public int RunningCount => _deviceManager.RunningCount;
    public bool CanModify => _authService.CurrentUser?.Role is "Admin" or "Operator";

    private readonly AuthService _authService;

    public MonitoringViewModel(DeviceManagerService deviceManager, IAlarmLogService alarmLogService,
                               AuthService authService)
    {
        _deviceManager = deviceManager;
        _alarmLogService = alarmLogService;
        _authService = authService;
        SelectedDevice = Devices.FirstOrDefault();
        _ = LoadAlarmHistoryAsync();
        _deviceManager.DeviceListChanged += () =>
        {
            Application.Current?.Dispatcher.Invoke(async () => await LoadAlarmHistoryAsync());
        };
    }

    [RelayCommand] private void AddDevice() => _deviceManager.AddDevice();
    [RelayCommand] private void RemoveDevice(DeviceViewModel? device) => _deviceManager.RemoveDevice(device);

    [RelayCommand] private async Task RefreshAlarms() => await LoadAlarmHistoryAsync();

    [RelayCommand] private void FirstPage() { _currentPage = 0; ApplyPaging(); }
    [RelayCommand] private void LastPage() { _currentPage = Math.Max(0, (_allAlarms.Count - 1) / PAGE_SIZE); ApplyPaging(); }
    [RelayCommand] private void PrevPage() { if (_currentPage > 0) _currentPage--; ApplyPaging(); }
    [RelayCommand] private void NextPage() { if ((_currentPage + 1) * PAGE_SIZE < _allAlarms.Count) _currentPage++; ApplyPaging(); }

    [RelayCommand]
    private void JumpPage(string? pageText)
    {
        if (int.TryParse(pageText, out int page) && page >= 1)
        {
            int totalPages = Math.Max(1, (_allAlarms.Count + PAGE_SIZE - 1) / PAGE_SIZE);
            _currentPage = Math.Clamp(page - 1, 0, totalPages - 1);
            ApplyPaging();
        }
    }

    private void ApplyPaging()
    {
        PagedAlarms.Clear();
        var items = _allAlarms.Skip(_currentPage * PAGE_SIZE).Take(PAGE_SIZE);
        foreach (var r in items) PagedAlarms.Add(r);
        int total = Math.Max(1, (_allAlarms.Count + PAGE_SIZE - 1) / PAGE_SIZE);
        PageInfo = $"第 {_currentPage + 1}/{total} 页 (共 {_allAlarms.Count} 条)";
    }

    private async Task LoadAlarmHistoryAsync()
    {
        var records = await _alarmLogService.GetAllAsync();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _allAlarms.Clear();
            _allAlarms.AddRange(records);
            ApplyPaging();
            OnPropertyChanged(nameof(RunningCount));
        });
    }
}
