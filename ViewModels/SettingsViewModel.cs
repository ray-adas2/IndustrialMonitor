using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace IndustrialMonitor.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IOptionsMonitor<MonitorConfig> _configMonitor;
    private readonly AuthService _authService;
    private readonly string _configPath;

    [ObservableProperty] private double _temperatureThreshold;
    [ObservableProperty] private double _pressureThreshold;
    [ObservableProperty] private int _refreshIntervalMs;
    [ObservableProperty] private int _slidingWindowSize;
    [ObservableProperty] private double _alarmDebounceSeconds;

    // 修改密码
    [ObservableProperty] private string _oldPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _passwordMessage = string.Empty;

    public SettingsViewModel(IOptionsMonitor<MonitorConfig> configMonitor, AuthService authService)
    {
        _configMonitor = configMonitor;
        _authService = authService;
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
        PasswordMessage = "配置已保存";
    }

    [RelayCommand]
    private void ChangePassword()
    {
        PasswordMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(OldPassword) ||
            string.IsNullOrWhiteSpace(NewPassword) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            PasswordMessage = "所有密码字段不能为空";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            PasswordMessage = "两次输入的新密码不一致";
            return;
        }
        if (NewPassword.Length < 4)
        {
            PasswordMessage = "新密码至少 4 位";
            return;
        }

        var user = _authService.CurrentUser;
        if (user is null) return;

        if (!_authService.Login(user.Username, OldPassword))
        {
            PasswordMessage = "旧密码错误";
            return;
        }

        _authService.ChangePassword(user.Id, NewPassword);
        PasswordMessage = "密码修改成功！下次登录请使用新密码";
        OldPassword = NewPassword = ConfirmPassword = string.Empty;
    }
}
