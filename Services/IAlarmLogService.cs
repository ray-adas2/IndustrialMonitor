using IndustrialMonitor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.Services
{
    public interface IAlarmLogService
    {
        Task InsertAsync(AlarmRecord record);
        Task<List<AlarmRecord>> GetAllAsync();
    }
}
