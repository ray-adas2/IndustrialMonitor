using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        //初始化数据源服务和UI定时器
        private readonly DispatcherTimer _timer;

        //数据源服务 SQLite服务(现在通过接口实现)
        private readonly IMockDataService _dataService;
        private readonly IAlarmLogService _alarmLogService;
        private readonly IOptionsMonitor<MonitorConfig> _configMonitor; // 🟢 热重载监视器

        // 2. 设备标识 (新增属性，方便标识每个设备)
        [ObservableProperty]
        private string _deviceId = "Dev-001";

        //累计产生的数据总点数，用来作为图表 X 轴的无尽自增序号
        private int _totalPointCount = 0;

        //防抖机制：记录上一次发生超标的时间
        private DateTime _lastViolationTime = DateTime.MinValue;

        //当前报警会话状态跟踪变量
        private DateTime _currentAlarmStartTime = DateTime.MinValue;
        private double _currentAlarmMaxTemp = 0;
        private double _currentAlarmMaxPress = 0;

        //LiveCharts绑定的数据源集合（专门放 X, Y 点）
        private readonly ObservableCollection<ObservablePoint> _tempPoints = new();
        private readonly ObservableCollection<ObservablePoint> _pressPoints = new();

        [ObservableProperty]
        private string _latestTemperature = "-- °C";

        [ObservableProperty]
        private string _latestPressure = "-- MPa";

        [ObservableProperty]
        private int _deviceCount;//当前数据点数

        //报警阈值配置 -动态属性：在配置更新时触发 PropertyChanged
        public double TemperatureThreshold => _configMonitor.CurrentValue.TemperatureThreshold;
        public double PressureThreshold => _configMonitor.CurrentValue.PressureThreshold;

        [ObservableProperty]
        private bool _isAlarming;

        [ObservableProperty]
        private string _alarmMessage = "系统正常运行";

        //表要绑定的数据源（UI线程用）
        public ObservableCollection<DeviceData> DataPoints { get; } = new();

        //暴露给XAML的图表 Series 属性
        public ISeries[] TemperatureSeries { get; set; }
        public ISeries[] PressureSeries { get; set; }

        //暴露给XAML的阈值辅助线属性
        public RectangularSection[] TemperatureSections { get; set; }
        public RectangularSection[] PressureSections { get; set; }

        //状态属性(控制按钮可用状态)
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        private bool _isRunning;

        // 新增：当触发报警并保存成功后，通知 MainViewModel 刷新全局日志表
        public Action? OnAlarmSaved { get; set; }

        public DeviceViewModel(IMockDataService dataService, IAlarmLogService alarmLogService, IOptionsMonitor<MonitorConfig> configMonitor)
        {
            //实现依赖注入
            _dataService = dataService;
            _alarmLogService = alarmLogService;
            _configMonitor = configMonitor;

            //初始化定时器：告诉它每隔 400 毫秒在 UI 线程上弹起一次
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_configMonitor.CurrentValue.RefreshIntervalMs)
            };
            // 注册事件监听器：每次时间到了，就去执行 OnTimerTick 方法
            _timer.Tick += OnTimerTick;

            //配置温度折线图
            TemperatureSeries = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = _tempPoints,
                    Name = "实时温度",
                    Stroke = new SolidColorPaint(SKColors.Red,2),//红色线条
                    GeometrySize = 0,//隐藏折线上的小圆点，提升渲染能力
                    Fill = new SolidColorPaint(SKColors.Red.WithAlpha(20))//半透明红色阴影填充
                }
            };

            //配置压力折线图
            PressureSeries = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = _pressPoints,
                    Name = "实时压力",
                    Stroke = new SolidColorPaint(SKColors.Blue, 2),
                    GeometrySize = 0,
                    Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(20))
                }
            };

            //配置温度安全警戒线
            TemperatureSections = new RectangularSection[]
            {
                new RectangularSection
                {
                    Yi = TemperatureThreshold,
                    Yj = TemperatureThreshold,
                    Stroke = new SolidColorPaint(SKColors.Red, 2)
                    {
                        // 🟢 改用 LiveCharts 自带的 DashEffect
                        PathEffect = new DashEffect(new float[] { 5, 5 })
                    }
                }
            };

            //配置压力安全警戒线
            PressureSections = new RectangularSection[]
            {
                new RectangularSection
                {
                    Yi = PressureThreshold,
                    Yj = PressureThreshold,
                    Stroke = new SolidColorPaint(SKColors.DarkOrange, 2)
                    {
                        // 🟢 改用 LiveCharts 自带的 DashEffect
                        PathEffect = new DashEffect(new float[] { 5, 5 })
                    }
                }
            };
            // 🟢 订阅配置文件热重载回调
            _configMonitor.OnChange(OnConfigChanged);
        }

        // 🟢 文件修改时的响应逻辑
        private void OnConfigChanged(MonitorConfig newConfig)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // 1. 动态改定时器频率
                _timer.Interval = TimeSpan.FromMilliseconds(newConfig.RefreshIntervalMs);

                // 2. 动态移动警戒线
                if (TemperatureSections != null && TemperatureSections.Length > 0)
                {
                    TemperatureSections[0].Yi = newConfig.TemperatureThreshold;
                    TemperatureSections[0].Yj = newConfig.TemperatureThreshold;
                }

                if (PressureSections != null && PressureSections.Length > 0)
                {
                    PressureSections[0].Yi = newConfig.PressureThreshold;
                    PressureSections[0].Yj = newConfig.PressureThreshold;
                }

                // 3. 通知 XAML 界面刷新绑定的数字
                OnPropertyChanged(nameof(TemperatureThreshold));
                OnPropertyChanged(nameof(PressureThreshold));
            });
        }

        //开始监控命令
        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            IsRunning = true;
            IsAlarming = false;
            AlarmMessage = "系统运行正常";
            _dataService.Start();
            _timer.Start();
        }

        

        private bool CanStart() => !IsRunning;

        //停止监控命令
        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            IsRunning = false;
            _dataService.Stop();
            _timer.Stop();
        }
        private bool CanStop() => IsRunning;

        //事件处理程序
        private async void OnTimerTick(object? sender, EventArgs e)
        {
            //用于找出本批次数据中的最大值，以实现最清爽的单条最严重警报展示
            double maxTempInBatch = 0;
            double maxPressInBatch = 0;

            // 🟢 每次 Tick 自动拿最新的 CurrentValue
            var currentConfig = _configMonitor.CurrentValue;

            while (_dataService.DataQueue.TryDequeue(out var data))
            {
                DataPoints.Add(data);
                _tempPoints.Add(new ObservablePoint(_totalPointCount, data.Temperature));
                _pressPoints.Add(new ObservablePoint(_totalPointCount, data.Pressure));
                _totalPointCount++;

                if (data.Temperature > maxTempInBatch) maxTempInBatch = data.Temperature;
                if (data.Pressure > maxPressInBatch) maxPressInBatch = data.Pressure;
            }


            //滑动窗口控流！为了防止软件开一天后内存爆炸，我们只在界面保留最近的(SlidingWindowSize)条数据
            while (DataPoints.Count > currentConfig.SlidingWindowSize)
            {
                //扔掉最旧的那条数据（先进先出）
                DataPoints.RemoveAt(0);
                _tempPoints.RemoveAt(0);
                _pressPoints.RemoveAt(0);
            }

            DeviceCount = DataPoints.Count;//更新数据

            //更新文本摘要
            if (DataPoints.Count > 0)
            {
                var latest = DataPoints[^1];// ^1 是 C# 的新语法，代表取最后（最新）一个元素
                LatestTemperature = latest.Temperature.ToString("F2") + " °C";
                LatestPressure = latest.Pressure.ToString("F3") + " MPa";
            }

            //报警引擎状态机与 3 秒防抖逻辑    阈值热重载生效
            bool tempError = maxTempInBatch > currentConfig.TemperatureThreshold;
            bool pressError = maxPressInBatch > currentConfig.PressureThreshold;

            if (tempError || pressError)
            {
                // 一旦有超标，立刻触发报警，更新最后一次违规时间戳
                _lastViolationTime = DateTime.Now;

                //报警会话起点检测
                if (!IsAlarming)
                {
                    _currentAlarmStartTime = DateTime.Now;
                    _currentAlarmMaxTemp = 0;
                    _currentAlarmMaxPress = 0;
                    IsAlarming = true;
                }

                //持续追踪并更新整个报警会话期间的最大值
                if (maxPressInBatch > _currentAlarmMaxPress)
                    _currentAlarmMaxPress = maxPressInBatch;
                if (maxTempInBatch > _currentAlarmMaxTemp)
                    _currentAlarmMaxTemp = maxTempInBatch;

                //拼装最严重报警信息
                var errors = new List<string>();
                if (tempError) errors.Add($"温度超标({maxTempInBatch:F1}°C > {TemperatureThreshold}°C)");
                if (pressError) errors.Add($"压力超标({maxPressInBatch:F3}MPa > {PressureThreshold}MPa)");

                AlarmMessage = "⚠️ 警报: " + string.Join(" | ", errors);
            }
            else
            {
                // 如果当前批次没有超标数据，检查是否处于“报警状态中”
                if (IsAlarming)
                {
                    var secondsSinceLastViolation = (DateTime.Now - _lastViolationTime).TotalSeconds;

                    //防抖秒数热重载生效
                    if (secondsSinceLastViolation >= currentConfig.AlarmDebounceSeconds)
                    {
                        // 已经平稳度过安全期，解除警报
                        IsAlarming = false;
                        AlarmMessage = "系统运行正常";

                        //异步组装日志写入SQLite
                        await SaveCurrentAlarmLogAsync();

                        // 🟢 回调通知 MainViewModel 重新加载历史表
                        OnAlarmSaved?.Invoke();
                    }
                    else
                    {
                        // 还在观察期内，保持警报并显示倒计时
                        double remaining = currentConfig.AlarmDebounceSeconds - secondsSinceLastViolation;
                        AlarmMessage = $"⚠️ 警报待解除，观察中... (剩余 {remaining:F1} 秒)";
                    }
                }
            }
        }


        //保存报警日志的方法
        private async Task SaveCurrentAlarmLogAsync()
        {
            if (_currentAlarmStartTime == DateTime.MinValue)
                return;

            string alarmType;
            double maxValue;
            double threshold;//阈值

            bool tempViolated = _currentAlarmMaxTemp > TemperatureThreshold;
            bool pressViolated = _currentAlarmMaxPress > PressureThreshold;

            //决定报警类型和保存的参数
            if (tempViolated && pressViolated)
            {
                alarmType = "温度+压力";
                maxValue = _currentAlarmMaxTemp; // 混合超标时，日志记录温度最大值，或也可以分别处理，这里采用记录温度
                threshold = TemperatureThreshold;
            }
            else if (tempViolated)
            {
                alarmType = "温度";
                maxValue = _currentAlarmMaxTemp;
                threshold = TemperatureThreshold;
            }
            else
            {
                alarmType = "压力";
                maxValue = _currentAlarmMaxPress;
                threshold = PressureThreshold;
            }

            var record = new AlarmRecord
            {
                DeviceId = DeviceId, // 新增（是改变，原本是固定设备名）🟢 改用当前设备自身的 DeviceId
                AlarmType = alarmType,
                MaxValue = maxValue,
                Threshold = threshold,
                StartTime = _currentAlarmStartTime,
                EndTime = DateTime.Now // 当前解除时间
            };
            await _alarmLogService.InsertAsync(record);
        }
    }
}
