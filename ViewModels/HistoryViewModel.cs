using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace IndustrialMonitor.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IAlarmLogService _alarmLogService;
    private readonly List<AlarmRecord> _allAlarms = new();
    private const int PAGE_SIZE = 15;
    private int _currentPage;

    public ObservableCollection<AlarmRecord> PagedRecords { get; } = new();

    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private string _selectedDeviceFilter = "全部";
    [ObservableProperty] private string _pageInfo = "第 0/0 页";
    [ObservableProperty] private string _summaryText = "";

    public ObservableCollection<string> DeviceFilters { get; } = new() { "全部" };

    public HistoryViewModel(IAlarmLogService alarmLogService)
    {
        _alarmLogService = alarmLogService;
        _ = LoadAsync();
    }

    [RelayCommand] private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Query()
    {
        _currentPage = 0;
        ApplyFilters();
        await Task.CompletedTask;
    }

    [RelayCommand] private void FirstPage() { _currentPage = 0; ApplyFilters(); }
    [RelayCommand] private void LastPage() { _currentPage = Math.Max(0, (FilteredList.Count - 1) / PAGE_SIZE); ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (_currentPage > 0) _currentPage--; ApplyFilters(); }
    [RelayCommand] private void NextPage() { if ((_currentPage + 1) * PAGE_SIZE < FilteredList.Count) _currentPage++; ApplyFilters(); }

    [RelayCommand]
    private void JumpPage(string? pageText)
    {
        if (int.TryParse(pageText, out int page) && page >= 1)
        {
            int totalPages = Math.Max(1, (FilteredList.Count + PAGE_SIZE - 1) / PAGE_SIZE);
            _currentPage = Math.Clamp(page - 1, 0, totalPages - 1);
            ApplyFilters();
        }
    }

    private List<AlarmRecord> FilteredList => _allAlarms
        .Where(a => a.StartTime >= StartDate && a.StartTime <= EndDate.AddDays(1))
        .Where(a => SelectedDeviceFilter == "全部" || a.DeviceId == SelectedDeviceFilter)
        .ToList();

    private void ApplyFilters()
    {
        var filtered = FilteredList;
        PagedRecords.Clear();
        var items = filtered.Skip(_currentPage * PAGE_SIZE).Take(PAGE_SIZE);
        foreach (var r in items) PagedRecords.Add(r);
        int total = Math.Max(1, (filtered.Count + PAGE_SIZE - 1) / PAGE_SIZE);
        PageInfo = $"第 {_currentPage + 1}/{total} 页 (共 {filtered.Count} 条)";
        int tempCount = filtered.Count(a => a.AlarmType.Contains("温度"));
        int pressCount = filtered.Count(a => a.AlarmType.Contains("压力"));
        SummaryText = $"时间段内共 {filtered.Count} 条报警 (温度: {tempCount}, 压力: {pressCount})";
    }

    private async Task LoadAsync()
    {
        var records = await _alarmLogService.GetAllAsync();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _allAlarms.Clear();
            _allAlarms.AddRange(records);
            // 收集唯一设备号
            var deviceIds = _allAlarms.Select(a => a.DeviceId).Distinct().ToList();
            DeviceFilters.Clear();
            DeviceFilters.Add("全部");
            foreach (var id in deviceIds) DeviceFilters.Add(id);
            ApplyFilters();
        });
    }
}
