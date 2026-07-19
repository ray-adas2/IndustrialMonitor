namespace IndustrialMonitor.Models
{
    public class AlarmRecord
    {
        public int Id { get; set; }                  //主键自增
        public string DeviceId { get; set; } = "";   //设备编号
        public string AlarmType { get; set; } = "";  //"温度" / "压力" / "温度+压力"
        public double MaxValue { get; set; }         //超标期间的最大值
        public double Threshold { get; set; }        //触发时的阈值
        public DateTime StartTime { get; set; }      //报警开始时间
        public DateTime EndTime { get; set; }        //报警解除时间
    }
}
