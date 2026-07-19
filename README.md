# IndustrialMonitor — 工业设备实时数据监控看板

基于 .NET 8 + WPF 的工业设备实时监控系统，采用 **MVVM + 依赖注入 + Service 层**架构。核心聚焦**高频数据流下的 UI 渲染优化**和**报警引擎设计**，适用于工业上位机 / IoT 监控场景。

## 功能

- **多设备管理**：动态添加 / 删除设备，TabControl 切换，每个设备独立监控
- **高频数据模拟**：生产者-消费者模式，后台线程模拟每秒数据推送，10% 概率产生超标异常
- **实时双通道折线图**：LiveCharts2 绑定 `ObservablePoint`，温度 / 压力独立曲线 + 阈值警戒虚线
- **限流采样渲染**：`DispatcherTimer` 每 400ms 批量出队数据，滑动窗口限制 50 个点，防止 UI 卡顿
- **报警引擎**：3 秒防抖状态机，报警灯呼吸闪烁动画，倒计时观察期显示
- **SQLite 报警存储**：报警解除后自动写入，`DataGrid` 展示历史记录，参数化查询
- **依赖注入**：`IServiceCollection` 容器管理依赖生命周期（Singleton / Transient），接口编程
- **配置热重载**：`appsettings.json` + `IOptionsMonitor<T>`，运行时修改阈值 / 刷新间隔 / 滑动窗口立即生效
- **报警灯呼吸动画**：WPF `Storyboard` + `DoubleAnimation`，DataTrigger 控制启停

## 项目架构

```
┌─────────────────────────────────────────────────────────────┐
│ App.xaml.cs (Composition Root)                              │
│   ConfigurationBuilder → appsettings.json (reloadOnChange)  │
│   ServiceCollection → DI Container                          │
│     Singleton: IAlarmLogService → AlarmLogService           │
│     Transient: IMockDataService → MockDataService           │
│     Singleton: MainViewModel                                │
│     Transient: DeviceViewModel (via Func<T> factory)        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ MainViewModel (设备管理器)                                    │
│   ObservableCollection<DeviceViewModel> Devices             │
│   ObservableCollection<AlarmRecord> AlarmHistory             │
│   RelayCommand: AddDevice / RemoveDevice                    │
│   OnAlarmSaved 回调解耦 ←→ DeviceViewModel                  │
└──────────────┬──────────────────────────────────────────────┘
               │ 1:N
               ▼
┌──────────────────────────────────────────────────────────────┐
│ DeviceViewModel (单设备监控，每个设备一个实例)                  │
│                                                              │
│  后台线程（生产者）:                 UI 线程（消费者）:         │
│  MockDataService.Start()            DispatcherTimer (400ms)  │
│    └→ ConcurrentQueue.Enqueue        └→ TryDequeue 批量出队   │
│       Thread.Sleep(500)                └→ ObservablePoint 图表 │
│       (90%正常 + 10%异常)               └→ 滑动窗口 RemoveAt(0) │
│                                         └→ 报警状态机          │
│                                            └→ SQLite 写入     │
│                                            └→ OnAlarmSaved?() │
└──────────────────────────────────────────────────────────────┘
```

## 项目结构

```
IndustrialMonitor/
├── Models/
│   ├── DeviceData.cs              # 设备数据点（普通 POCO）
│   ├── AlarmRecord.cs             # 报警记录（SQLite 表映射）
│   └── MonitorConfig.cs           # 配置强类型模型
├── Services/
│   ├── IMockDataService.cs        # 数据模拟器接口
│   ├── IAlarmLogService.cs        # 报警日志接口
│   ├── MockDataService.cs         # 高频数据模拟实现
│   └── AlarmLogService.cs         # SQLite 增删查实现
├── ViewModels/
│   ├── DeviceViewModel.cs         # 单设备 ViewModel（361 行）
│   └── MainViewModel.cs           # 设备管理器 ViewModel（90 行）
├── MainWindow.xaml                # 主界面（5 行布局）
├── MainWindow.xaml.cs             # 窗口入口（构造注入）
├── App.xaml                       # 应用配置
├── App.xaml.cs                    # DI 容器 + 配置加载
└── appsettings.json               # 可热重载配置
```

## 关键技术实现

### 生产者-消费者 + 限流渲染

解决「每秒 200 条数据冲击 UI 线程」的核心架构：

```csharp
// === 生产者（后台线程）===
// MockDataService.Start():
Task.Run(() => {
    while (_isRunning)
    {
        _dataQueue.Enqueue(new DeviceData { ... });
        Thread.Sleep(500);  // 每 500ms 一条
    }
});

// === 消费者（UI 线程）===
// DeviceViewModel.OnTimerTick() — DispatcherTimer 每 400ms 触发：
while (_dataService.DataQueue.TryDequeue(out var data))
{
    DataPoints.Add(data);                              // 文本摘要数据源
    _tempPoints.Add(new ObservablePoint(...));          // 图表数据源
    // 更新本批次最大值
}
// 滑动窗口：只保留最近 50 个点
while (DataPoints.Count > 50)
    DataPoints.RemoveAt(0);
```

`ConcurrentQueue` 作为线程安全缓冲区隔离数据生产与 UI 消费，`DispatcherTimer` 按固定频率批量取数，确保 UI 帧率稳定。

### 报警引擎 3 秒防抖状态机

```
正常 ──超标发生──→ 报警 (IsAlarming = true, 记录会话起点)
                    │
                    ├← 持续超标 → 保持报警，追踪峰值
                    │
                    └← 恢复正常 → 启动观察期 (3 秒)
                                    │
                                    ├ 3 秒内再次超标 → 回到报警状态
                                    └ 3 秒无超标 → 解除报警 → 写入 SQLite
```

```csharp
if (maxTempInBatch > threshold || maxPressInBatch > threshold)
{
    _lastViolationTime = DateTime.Now;
    if (!IsAlarming)  // 首次触发：记录会话起点
    {
        _currentAlarmStartTime = DateTime.Now;
        _currentAlarmMaxTemp = _currentAlarmMaxPress = 0;
        IsAlarming = true;
    }
    // 持续追踪会话期间峰值
    _currentAlarmMaxTemp = Math.Max(_currentAlarmMaxTemp, maxTempInBatch);
}
else if (IsAlarming)
{
    if ((DateTime.Now - _lastViolationTime).TotalSeconds >= 3.0)
    {
        IsAlarming = false;
        await SaveCurrentAlarmLogAsync();  // 解除时异步写 SQLite
    }
    else
        AlarmMessage = $"观察中... (剩余 {3.0 - elapsed:F1} 秒)";
}
```

### 依赖注入 + 工厂模式

```csharp
// App.xaml.cs — Composition Root
var services = new ServiceCollection();
services.Configure<MonitorConfig>(config.GetSection("MonitorConfig"));
services.AddSingleton<IAlarmLogService, AlarmLogService>();
services.AddTransient<IMockDataService, MockDataService>();
services.AddSingleton<MainViewModel>();
services.AddTransient<DeviceViewModel>();
// Func<T> 工厂：MainViewModel 动态创建设备而不直接依赖 DI 容器
services.AddSingleton<Func<DeviceViewModel>>(
    provider => () => provider.GetRequiredService<DeviceViewModel>());

// MainViewModel 通过构造函数接收工厂
public MainViewModel(Func<DeviceViewModel> deviceFactory) { ... }

// 运行时动态创建
private void AddDevice()
{
    var device = _deviceFactory();  // DI 自动注入所有依赖
    Devices.Add(device);
}
```

### 配置热重载

```csharp
// 注册 IOptionsMonitor（支持 reloadOnChange）
services.Configure<MonitorConfig>(config.GetSection("MonitorConfig"));

// DeviceViewModel 注入 IOptionsMonitor<MonitorConfig>
public double TemperatureThreshold =>
    _configMonitor.CurrentValue.TemperatureThreshold;

// 订阅文件变化 → 实时更新 UI
_configMonitor.OnChange(newConfig =>
{
    Application.Current?.Dispatcher.Invoke(() =>
    {
        _timer.Interval = TimeSpan.FromMilliseconds(newConfig.RefreshIntervalMs);
        TemperatureSections[0].Yi = newConfig.TemperatureThreshold;  // 图表阈值线移动
        OnPropertyChanged(nameof(TemperatureThreshold));              // XAML 绑定刷新
    });
});
```

为了开发时编辑项目源文件即生效，同时监视两个路径：
```csharp
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
.AddJsonFile("../../../appsettings.json", optional: true, reloadOnChange: true)
```

### LiveCharts2 图表集成

```csharp
// 折线图配置
TemperatureSeries = new ISeries[] {
    new LineSeries<ObservablePoint> {
        Values = _tempPoints,                               // ObservableCollection 绑定
        Stroke = new SolidColorPaint(SKColors.Red, 2),      // 红色，2px
        GeometrySize = 0,                                   // 隐藏数据点圆球（性能优化）
        Fill = new SolidColorPaint(SKColors.Red.WithAlpha(20))  // 半透明填充
    }
};

// 阈值警戒虚线
TemperatureSections = new RectangularSection[] {
    new RectangularSection {
        Yi = 85.0, Yj = 85.0,
        Stroke = new SolidColorPaint(SKColors.Red, 2) {
            PathEffect = new DashEffect(new float[] { 5, 5 })  // 虚线
        }
    }
};
```

### TabControl + ContentTemplate 绑定

```xml
<TabControl ItemsSource="{Binding Devices}" SelectedItem="{Binding SelectedDevice}">
    <TabControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding DeviceId}"/>  <!-- Tab 标签 -->
        </DataTemplate>
    </TabControl.ItemTemplate>
    <TabControl.ContentTemplate>
        <DataTemplate>
            <!-- DataContext 自动指向当前 DeviceViewModel，无需 SelectedDevice. 前缀 -->
            <lvc:CartesianChart Series="{Binding TemperatureSeries}"
                                Sections="{Binding TemperatureSections}"/>
        </DataTemplate>
    </TabControl.ContentTemplate>
</TabControl>
```

### SQLite 参数化查询

```csharp
// 建表（构造函数自动执行）
var sql = @"CREATE TABLE IF NOT EXISTS AlarmRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DeviceId TEXT NOT NULL, AlarmType TEXT NOT NULL,
    MaxValue REAL NOT NULL, Threshold REAL NOT NULL,
    StartTime TEXT NOT NULL, EndTime TEXT NOT NULL);";

// 参数化插入（防 SQL 注入）
command.Parameters.AddWithValue("$deviceId", record.DeviceId);
command.Parameters.AddWithValue("$startTime", record.StartTime.ToString("o"));
```

## 依赖

| 包 | 版本 | 用途 |
|----|------|------|
| `CommunityToolkit.Mvvm` | 8.4.2 | MVVM 基础设施 |
| `LiveChartsCore.SkiaSharpView.WPF` | 2.0.5 | 实时折线图 |
| `Microsoft.Data.Sqlite` | 10.0.10 | SQLite 数据库 |
| `Microsoft.Extensions.DependencyInjection` | 10.0.10 | DI 容器 |
| `Microsoft.Extensions.Configuration.Json` | 10.0.10 | JSON 配置读取 |
| `Microsoft.Extensions.Configuration.Binder` | 10.0.10 | 配置绑定到对象 |
| `Microsoft.Extensions.Options` | 10.0.10 | IOptionsMonitor 热重载 |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 10.0.10 | services.Configure<T> |

## 配置

所有运行时可调参数集中在 `appsettings.json`，支持热重载：

```json
{
  "MonitorConfig": {
    "TemperatureThreshold": 85.0,
    "PressureThreshold": 2.4,
    "RefreshIntervalMs": 400,
    "SlidingWindowSize": 50,
    "AlarmDebounceSeconds": 3.0
  }
}
```

## 运行

```bash
dotnet run
```

或打开 `IndustrialMonitor.sln`，在 Visual Studio / Rider 中按 F5。
