using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialMonitor.Converters;

/// <summary>
/// 报警类型 → Tag 背景色：温度=红色, 压力=蓝色, 温度+压力=紫色
/// </summary>
public class AlarmTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var alarmType = value as string ?? string.Empty;
        var color = alarmType switch
        {
            "温度" => (Brush)new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            "压力" => (Brush)new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
            "温度+压力" => (Brush)new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)),
            _ => (Brush)new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))
        };
        return color;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
