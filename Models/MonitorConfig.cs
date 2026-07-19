namespace IndustrialMonitor.Models
{
    //强类型配置映射类
    public class MonitorConfig
    {
        public double TemperatureThreshold { get; set; } = 85.0;
        public double PressureThreshold { get; set; } = 2.4;
        public int RefreshIntervalMs { get; set; } = 400;//UI刷新间隔
        public int SlidingWindowSize { get; set; } = 50;//页面显示数据数量
        public double AlarmDebounceSeconds { get; set; } = 3.0;//报警解除时间
    }
}
