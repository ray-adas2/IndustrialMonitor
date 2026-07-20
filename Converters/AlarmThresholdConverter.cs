using System;
using System.Globalization;
using System.Windows.Data;

namespace IndustrialMonitor.Converters;

public class AlarmThresholdConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string alarmType || values[1] is not double threshold)
            return "—";

        return alarmType switch
        {
            "温度" => $"温度 {threshold:F1} °C",
            "压力" => $"压力 {threshold:F3} MPa",
            "温度+压力" => $"温度 {threshold:F1} °C + 压力超标",
            _ => $"{threshold}"
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
