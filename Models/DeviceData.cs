namespace IndustrialMonitor.Models
{
    public class DeviceData
    {
        //设备编号
        public string DeviceId { get; set; } = string.Empty;
        
        //温度
        public double Temperature { get; set; }
        
        //压力
        public double Pressure { get; set; }
        
        //时间戳
        public  DateTime Timestamp { get; set; }
    }
}
