using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace IndustrialMonitor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        //构建Configuration 读取appsettings.json文件
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            // 开发时同时监视项目源文件（改动立即生效，不用等编译）
            .AddJsonFile(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "appsettings.json"),
                optional: true,
                reloadOnChange: true);

        var config = builder.Build();


        // 1. 构建服务注册表（Service Registry）
        // 此处不产生任何实例，仅将接口-实现-生命周期三元组写入内存集合。
        var services = new ServiceCollection();
        ConfigureServices(services,config);

        // 2. 构建服务提供者（Build Service Provider）
        // 将注册表编译为运行时容器。此操作会验证循环依赖，并生成对象工厂。
        // ServiceProvider 作为 Composition Root（组合根），是应用中所有对象图的唯一来源。
        ServiceProvider = services.BuildServiceProvider();

        // 3. 服务解析（Service Resolution）
        // 从容器中解析应用程序的根对象（Root Object）。
        // 容器递归执行构造函数注入（Constructor Injection），构建完整的对象图（Object Graph）。
        // GetRequiredService 确保根对象必然存在，否则应用程序无法正常启动。
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

        // 4. 呈现 UI
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Microsoft.Extensions.Options.ConfigurationExtensions缺少这个包导致报错
        //Configure<T>(IConfigurationSection)（从 JSON 绑定）
        //Configure<T>(Action<T>)（手动赋值）来自Microsoft.Extensions.DependencyInjection.Abstractions
        services.Configure<MonitorConfig>(configuration.GetSection("MonitorConfig"));


        //注册服务（接口->实现）
        services.AddSingleton<IAlarmLogService, AlarmLogService>();
        services.AddTransient<IMockDataService, MockDataService>();

        // 设备管理服务（全局单例，跨页面共享）
        services.AddSingleton<DeviceManagerService>();

        // Shell 层 ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<NavigationViewModel>();

        // 页面 ViewModels（每个页面一个实例）
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<DeviceManagementViewModel>();
        services.AddSingleton<MonitoringViewModel>();
        services.AddSingleton<AlarmCenterViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // 设备 ViewModel（Transient：每个设备独立实例）
        services.AddTransient<DeviceViewModel>();
        services.AddSingleton<Func<DeviceViewModel>>(provider => () => provider.GetRequiredService<DeviceViewModel>());

        //注册Views
        services.AddSingleton<MainWindow>();

    }
}

