using IndustrialMonitor.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace IndustrialMonitor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;
    private bool _isReallyClosing;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // 系统托盘图标
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = global::System.Drawing.Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            Text = "工业监控平台",
            Visible = true
        };

        // 右键菜单
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
        menu.Items.Add("退出登录", null, (s, e) => { Show(); _viewModel.LogoutCommand.Execute(null); });
        menu.Items.Add("-");
        menu.Items.Add("关闭系统", null, (s, e) =>
        {
            _isReallyClosing = true;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        });
        _trayIcon.ContextMenuStrip = menu;

        // 双击托盘图标
        _trayIcon.DoubleClick += (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); };

        // 点 X 最小化到托盘
        Closing += (s, e) =>
        {
            if (!_isReallyClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_isReallyClosing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnClosed(e);
    }
}
