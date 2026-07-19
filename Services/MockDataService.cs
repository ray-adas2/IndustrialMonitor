using IndustrialMonitor.Models;
using System.Collections.Concurrent;

namespace IndustrialMonitor.Services
{
    public class MockDataService:IMockDataService
    {
        //线程安全的传送带（并发队列）ConcurrentQueue(专门用于多线程的队列)
        private readonly ConcurrentQueue<DeviceData> _dataQueue = new();
        private bool _isRunning;//机器电源开关
        private readonly Random _random = new();//随机数生成器(模拟硬件数据)

        //传送带出口(公开的只读属性)
        public ConcurrentQueue<DeviceData> DataQueue => _dataQueue;


        public void Start()
        {
            if(_isRunning) return;

            _isRunning = true;//开启电源

            //开启后台线程
            Task.Run(() =>
            {
                while (_isRunning)
                {
                    // 🟢 工业常态模拟：
                    // 90% 概率处于安全状态，10% 概率突发异常超标
                    bool isAnomaly = _random.Next(0, 100) < 10;

                    double temp;
                    double press;

                    if (isAnomaly)
                    {
                        // 🚨 异常状态：温度 86°C ~ 95°C (阈值 85), 压力 2.45MPa ~ 2.65MPa (阈值 2.4)
                        temp = 86.0 + _random.NextDouble() * 9.0;
                        press = 2.45 + _random.NextDouble() * 0.2;
                    }
                    else
                    {
                        // 🟢 正常常态：温度 65°C ~ 82°C, 压力 1.8MPa ~ 2.3MPa
                        temp = 65.0 + _random.NextDouble() * 17.0;
                        press = 1.8 + _random.NextDouble() * 0.5;
                    }

                    // 创建一个新数据包裹
                    var data = new DeviceData
                    {
                        DeviceId = "Dev-001",
                        Temperature = temp,
                        Pressure = press,
                        Timestamp = DateTime.Now
                    };

                    _dataQueue.Enqueue(data); // 放入队列

                    // 🟢 500 毫秒生产一次，配合 UI 端的 500 毫秒消费，达到完美平衡
                    Thread.Sleep(500);
                }
            });
        }

        //关闭电源
        public void Stop()
        {
            _isRunning = false;
        }
    }
}
