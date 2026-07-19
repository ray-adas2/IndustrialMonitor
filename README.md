# IndustrialMonitor — 工业数据实时监控看板

> 🏭 我的第三个 WPF 学习项目，**简历级企业应用**，聚焦多线程、实时图表、报警引擎和依赖注入。

## 功能

- ✅ 多设备管理（动态添加/删除，TabControl 切换）
- ✅ 实时数据模拟（生产者-消费者模式，90% 正常 + 10% 异常）
- ✅ 双通道实时折线图（温度 + 压力，含阈值警戒虚线）
- ✅ 报警引擎（3 秒防抖机制，报警灯呼吸闪烁，倒计时观察期）
- ✅ SQLite 报警存储（持久化 + 历史查询 DataGrid）
- ✅ 依赖注入（IServiceCollection + 接口编程 + Func<T> 工厂模式）
- ✅ 配置热重载（appsettings.json，运行时修改立即生效）

## 技术栈

| 技术 | 作用 |
|------|------|
| **WPF (.NET 8)** | 桌面 UI 框架 |
| **CommunityToolkit.Mvvm** | MVVM 基础设施 |
| **LiveCharts2** | 实时折线图渲染 |
| **Microsoft.Data.Sqlite** | SQLite 数据库 |
| **Microsoft.Extensions.DependencyInjection** | DI 容器 |
| **Microsoft.Extensions.Configuration** | 配置系统 + 热重载 |
| **System.Threading.Channels** | 预备——高性能异步管道 |
| **System.Collections.Concurrent** | ConcurrentQueue 生产者-消费者 |

## 项目结构

```
IndustrialMonitor/
├── Models/
│   ├── DeviceData.cs              # 设备数据点
│   ├── AlarmRecord.cs             # 报警记录
│   └── MonitorConfig.cs           # 配置模型
├── Services/
│   ├── IMockDataService.cs        # 数据模拟器接口
│   ├── IAlarmLogService.cs        # 报警服务接口
│   ├── MockDataService.cs         # 高频数据模拟
│   └── AlarmLogService.cs         # SQLite 报警存储
├── ViewModels/
│   ├── DeviceViewModel.cs         # 单设备监控逻辑
│   └── MainViewModel.cs           # 设备管理器
├── MainWindow.xaml                # 主界面（TabControl + 图表）
├── App.xaml.cs                    # DI 容器启动
└── appsettings.json               # 可热重载的配置
```

## 运行

```bash
dotnet run
```

或在 Visual Studio 中打开 `IndustrialMonitor.sln` 按 F5 运行。

## 学习笔记

详见 [项目复习总结.md](项目复习总结.md)，包含架构演进、数据流图解、面试问答和完整踩坑记录。
