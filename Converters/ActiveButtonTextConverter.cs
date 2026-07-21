using System;
using System.Globalization;
using System.Windows.Data;

namespace IndustrialMonitor.Converters;

public class ActiveButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "禁用" : "启用";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
