using IndustrialMonitor.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.Services
{
    public class AlarmLogService:IAlarmLogService
    {
        //数据库文件名，运行后会在程序目录下生成
        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"alarms.db");
        private readonly string _connectionString;

        public AlarmLogService()
        {
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }

        //自动建表：如果数据库或表不存在，启动时自动创建
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS AlarmRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeviceId TEXT NOT NULL,
                    AlarmType TEXT NOT NULL,
                    MaxValue REAL NOT NULL,
                    Threshold REAL NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL
                );";

            using var command = new SqliteCommand(createTableSql,connection);
            command.ExecuteNonQuery();//执行非查询SQL
        }


        //插入报警记录
        public async Task InsertAsync(AlarmRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var insertSql = @"
                INSERT INTO AlarmRecords (DeviceId, AlarmType, MaxValue, Threshold, StartTime, EndTime)
                VALUES ($deviceId, $alarmType, $maxValue, $threshold, $startTime, $endTime);";

            using var command = new SqliteCommand(insertSql,connection);
            command.Parameters.AddWithValue("$deviceId",record.DeviceId);
            command.Parameters.AddWithValue("$alarmType", record.AlarmType);
            command.Parameters.AddWithValue("$maxValue", record.MaxValue);
            command.Parameters.AddWithValue("$threshold", record.Threshold);
            command.Parameters.AddWithValue("$startTime", record.StartTime.ToString("o"));
            command.Parameters.AddWithValue("$endTime", record.EndTime.ToString("o"));

            await command.ExecuteNonQueryAsync();
        }

        //查询所有报警记录
        public async Task<List<AlarmRecord>> GetAllAsync()
        {
            var list = new List<AlarmRecord>();

            using var connection = new SqliteConnection(_connectionString); 
            await connection.OpenAsync();

            var querySql = "SELECT * FROM AlarmRecords ORDER BY StartTime DESC;";
            using var command = new SqliteCommand(querySql,connection);
            using var reader = await command.ExecuteReaderAsync();//执行查询SQL

            while (await reader.ReadAsync())
            {
                list.Add(new AlarmRecord
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetString(1),
                    AlarmType = reader.GetString(2),
                    MaxValue = reader.GetDouble(3),
                    Threshold = reader.GetDouble(4),
                    StartTime = DateTime.Parse(reader.GetString(5)),
                    EndTime = DateTime.Parse(reader.GetString(6))
                });
            }

            return list;
        }
    }
}
