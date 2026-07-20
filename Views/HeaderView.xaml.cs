using System.Windows.Controls;
using System.Windows.Threading;

namespace IndustrialMonitor.Views;

public partial class HeaderView : UserControl
{
    private readonly DispatcherTimer _clockTimer;

    public HeaderView()
    {
        InitializeComponent();
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) =>
        {
            ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        };
        _clockTimer.Start();
    }
}
