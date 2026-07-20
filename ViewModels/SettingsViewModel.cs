using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;

namespace IndustrialMonitor.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IOptionsMonitor<MonitorConfig> _configMonitor;
    private readonly string _configPath;

    [ObservableProperty] private double _temperatureThreshold;
    [ObservableProperty] private double _pressureThreshold;
    [ObservableProperty] private int _refreshIntervalMs;
    [ObservableProperty] private int _slidingWindowSize;
    [ObservableProperty] private double _alarmDebounceSeconds;

    public SettingsViewModel(IOptionsMonitor<MonitorConfig> configMonitor)
    {
        _configMonitor = configMonitor;
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "appsettings.json");

        var cfg = configMonitor.CurrentValue;
        TemperatureThreshold = cfg.TemperatureThreshold;
        PressureThreshold = cfg.PressureThreshold;
        RefreshIntervalMs = cfg.RefreshIntervalMs;
        SlidingWindowSize = cfg.SlidingWindowSize;
        AlarmDebounceSeconds = cfg.AlarmDebounceSeconds;
    }

    [RelayCommand]
    private void Save()
    {
        var json = JsonSerializer.Serialize(
            new { MonitorConfig = new { TemperatureThreshold, PressureThreshold, RefreshIntervalMs, SlidingWindowSize, AlarmDebounceSeconds } },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
