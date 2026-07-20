using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace IndustrialMonitor.ViewModels;

public partial class AlarmCenterViewModel : ObservableObject
{
    private readonly IAlarmLogService _alarmLogService;
    private readonly List<AlarmRecord> _allAlarms = new();
    private const int PAGE_SIZE = 15;
    private int _currentPage;

    public ObservableCollection<AlarmRecord> PagedAlarms { get; } = new();
    [ObservableProperty] private string _pageInfo = "第 0/0 页";
    [ObservableProperty] private string _filterType = "全部";

    public List<string> FilterTypes { get; } = new() { "全部", "温度", "压力", "温度+压力" };

    public AlarmCenterViewModel(IAlarmLogService alarmLogService)
    {
        _alarmLogService = alarmLogService;
        _ = LoadAlarmsAsync();
    }

    [RelayCommand] private async Task Refresh() => await LoadAlarmsAsync();

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

    [RelayCommand]
    private void FilterByType(string? type)
    {
        FilterType = type ?? "全部";
        _currentPage = 0;
        ApplyPaging();
    }

    private List<AlarmRecord> FilteredList => FilterType switch
    {
        "温度" => _allAlarms.Where(a => a.AlarmType == "温度").ToList(),
        "压力" => _allAlarms.Where(a => a.AlarmType == "压力").ToList(),
        "温度+压力" => _allAlarms.Where(a => a.AlarmType == "温度+压力").ToList(),
        _ => _allAlarms
    };

    private void ApplyPaging()
    {
        PagedAlarms.Clear();
        var filtered = FilteredList;
        var items = filtered.Skip(_currentPage * PAGE_SIZE).Take(PAGE_SIZE);
        foreach (var r in items) PagedAlarms.Add(r);
        int total = Math.Max(1, (filtered.Count + PAGE_SIZE - 1) / PAGE_SIZE);
        PageInfo = $"第 {_currentPage + 1}/{total} 页 (共 {filtered.Count} 条)";
    }

    public async Task LoadAlarmsAsync()
    {
        var records = await _alarmLogService.GetAllAsync();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _allAlarms.Clear();
            _allAlarms.AddRange(records);
            _currentPage = 0;
            ApplyPaging();
        });
    }
}
