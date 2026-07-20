namespace IndustrialMonitor.Models;

/// <summary>
/// 导航菜单项模型
/// </summary>
public class NavMenuItem
{
    /// <summary>菜单图标（Emoji 或 Unicode 字符）</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>菜单显示文字</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>菜单唯一标识</summary>
    public string Key { get; set; } = string.Empty;
}
