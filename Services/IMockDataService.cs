using IndustrialMonitor.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.Services
{
    public interface IMockDataService
    {
        ConcurrentQueue<DeviceData> DataQueue { get; }
        void Start();
        void Stop();
    }
}
