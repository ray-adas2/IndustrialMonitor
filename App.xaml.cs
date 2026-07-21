using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
using IndustrialMonitor.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace IndustrialMonitor;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "appsettings.json"),
                optional: true,
                reloadOnChange: true);

        var config = builder.Build();

        var services = new ServiceCollection();
        ConfigureServices(services, config);
        ServiceProvider = services.BuildServiceProvider();

        // 防止 LoginWindow 关闭时触发自动关机
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
        var result = loginWindow.ShowDialog();

        if (result == true)
        {
            // 登录成功 → 打开主窗口，恢复正常关机模式
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        else
        {
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonitorConfig>(configuration.GetSection("MonitorConfig"));

        // 基础服务
        services.AddSingleton<IAlarmLogService, AlarmLogService>();
        services.AddTransient<IMockDataService, MockDataService>();
        services.AddSingleton<DeviceManagerService>();
        services.AddSingleton<AuthService>();

        // Shell 层
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<NavigationViewModel>();

        // 页面 ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<DeviceManagementViewModel>();
        services.AddSingleton<MonitoringViewModel>();
        services.AddSingleton<AlarmCenterViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<UserManagementViewModel>();

        // 设备
        services.AddTransient<DeviceViewModel>();
        services.AddSingleton<Func<DeviceViewModel>>(provider => () => provider.GetRequiredService<DeviceViewModel>());

        // 登录
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();

        // 主窗口
        services.AddSingleton<MainWindow>();
    }
}
