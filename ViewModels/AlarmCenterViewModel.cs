using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace IndustrialMonitor.ViewModels;

public partial class AlarmCenterViewModel : ObservableObject
{
    private readonly IAlarmLogService _alarmLogService;

    public ObservableCollection<AlarmRecord> Alarms { get; } = new();

    [ObservableProperty] private string _filterType = "全部";
    [ObservableProperty] private string _searchText = string.Empty;

    public AlarmCenterViewModel(IAlarmLogService alarmLogService)
    {
        _alarmLogService = alarmLogService;
        _ = LoadAlarmsAsync();
    }

    public async Task LoadAlarmsAsync()
    {
        var records = await _alarmLogService.GetAllAsync();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Alarms.Clear();
            foreach (var r in records) Alarms.Add(r);
        });
    }
}
