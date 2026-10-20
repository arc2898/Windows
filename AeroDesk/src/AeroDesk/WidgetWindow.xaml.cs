using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AeroDesk.Models;
using AeroDesk.Services;

namespace AeroDesk;

public partial class WidgetWindow : Window
{
    private readonly WidgetRecord _record;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _noteDebounce;
    private TimeSpan _previousCpu;
    private DateTime _previousSample = DateTime.UtcNow;
    private (long Received, long Sent) _lastNetwork;
    private DateTime _lastNetworkSample = DateTime.UtcNow;
    private bool _initializingNotes = true;
    public event EventHandler<string>? NoteChanged;
    public event EventHandler? WidgetClosed;

    public WidgetWindow(WidgetRecord record, AppSettings settings)
    {
        InitializeComponent();
        _record = record;
        Width = Math.Clamp(record.Width, 190, 600); Height = Math.Clamp(record.Height, 110, 600);
        Left = Math.Clamp(record.Left, 0, Math.Max(0, SystemParameters.WorkArea.Width - Width));
        Top = Math.Clamp(record.Top, 0, Math.Max(0, SystemParameters.WorkArea.Height - Height));
        Topmost = settings.KeepWidgetsOnTop;
        ShowActivated = false;
        ConfigureKind(record.Kind);
        _initializingNotes = false;
        _noteDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _noteDebounce.Tick += (_, _) => { _noteDebounce.Stop(); NoteChanged?.Invoke(this, NotesText.Text); };
        _timer = new DispatcherTimer { Interval = record.Kind == "clock" ? TimeSpan.FromSeconds(1) : record.Kind == "network" ? TimeSpan.FromSeconds(2) : record.Kind == "battery" ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(5) };
        _timer.Tick += (_, _) => RefreshContent();
        _lastNetwork = record.Kind == "network" ? SystemMetrics.NetworkTotals() : default;
        RefreshContent();
        SetOpacity(settings.WidgetOpacity);
    }

    private void ConfigureKind(string kind)
    {
        Heading.Text = WidgetKinds.Label(kind).ToUpperInvariant();
        FrameworkElement panel = kind switch
        {
            "calendar" => CalendarPanel, "notes" => NotesText, "system" => SystemPanel,
            "network" => NetworkPanel, "battery" => BatteryPanel, _ => ClockPanel
        };
        panel.Visibility = Visibility.Visible;
        if (kind == "notes") { Width = Math.Max(280, Width); Height = Math.Max(230, Height); NotesText.Text = _record.NoteText; }
        if (kind == "calendar") { Width = Math.Max(230, Width); Height = Math.Max(180, Height); }
        if (kind == "system") Height = Math.Max(190, Height);
        ContextMenu = new ContextMenu();
        var pin = new MenuItem { Header = "Keep above windows", IsCheckable = true, IsChecked = Topmost };
        pin.Click += (_, _) => Topmost = pin.IsChecked;
        var opacityMenu = new MenuItem { Header = "Reset size and position" };
        opacityMenu.Click += (_, _) => { Width = kind == "notes" ? 280 : 260; Height = kind == "notes" ? 230 : 160; Left = Math.Max(0, SystemParameters.WorkArea.Width - Width - 24); Top = 70; };
        var open = new MenuItem { Header = "Open AeroDesk" }; open.Click += (_, _) => App.ShowDashboard();
        var close = new MenuItem { Header = "Close widget" }; close.Click += (_, _) => Close();
        ContextMenu.Items.Add(pin); ContextMenu.Items.Add(opacityMenu); ContextMenu.Items.Add(open); ContextMenu.Items.Add(new Separator()); ContextMenu.Items.Add(close);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        GlassEffect.Apply(this, App.CurrentSettings.UseAcrylic, (byte)(App.CurrentSettings.WidgetOpacity * 255));
        _timer.Start();
    }
    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.KeepWidgetsOnTop;
        SetOpacity(settings.WidgetOpacity);
        GlassEffect.Apply(this, settings.UseAcrylic, (byte)(settings.WidgetOpacity * 255));
    }
    private void SetOpacity(double opacity)
    {
        var alpha = (byte)Math.Clamp((int)(opacity * 255), 70, 255);
        Panel.Background = new SolidColorBrush(Color.FromArgb(alpha, 26, 44, 65));
    }

    private void RefreshContent()
    {
        var now = DateTime.Now;
        switch (_record.Kind)
        {
            case "clock": ClockText.Text = now.ToString("HH:mm:ss"); ClockDate.Text = now.ToString("dddd, MMMM d"); break;
            case "calendar": CalendarDay.Text = now.ToString("dd"); CalendarMonth.Text = now.ToString("MMMM yyyy"); CalendarWeekday.Text = now.ToString("dddd"); break;
            case "system":
                var memory = SystemMetrics.Memory(); MemoryValue.Text = $"{memory.UsedPercent}%"; MemoryBar.Value = memory.UsedPercent;
                MemoryDetail.Text = $"{memory.Used} used of {memory.Total}";
                using var process = Process.GetCurrentProcess(); var stamp = DateTime.UtcNow; var cpu = process.TotalProcessorTime;
                var elapsed = Math.Max(0.01, (stamp - _previousSample).TotalSeconds);
                var cpuPct = Math.Clamp((cpu - _previousCpu).TotalMilliseconds / (elapsed * Environment.ProcessorCount * 10), 0, 100);
                ProcessDetail.Text = $"AeroDesk CPU: {cpuPct:0.0}%  •  {Environment.ProcessorCount} logical cores";
                _previousCpu = cpu; _previousSample = stamp; break;
            case "network":
                var net = SystemMetrics.NetworkTotals(); var sec = Math.Max(0.1, (DateTime.UtcNow - _lastNetworkSample).TotalSeconds);
                RxRate.Text = SystemMetrics.FormatBytes((long)(Math.Max(0, net.Received - _lastNetwork.Received) / sec)) + "/s";
                TxRate.Text = SystemMetrics.FormatBytes((long)(Math.Max(0, net.Sent - _lastNetwork.Sent) / sec)) + "/s";
                _lastNetwork = net; _lastNetworkSample = DateTime.UtcNow; break;
            case "battery":
                var b = SystemMetrics.Battery(); BatteryValue.Text = b.Present ? $"{b.Percent}%" : "No battery";
                BatteryStatus.Text = !b.Present ? "This device may be a desktop PC" : b.Charging ? "Charging" : "On battery";
                BatteryBar.Value = Math.Max(0, b.Percent); break;
        }
    }
    private void NotesText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializingNotes || _noteDebounce is null) return;
        _noteDebounce.Stop(); _noteDebounce.Start();
    }
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 1)
        {
            try { DragMove(); } catch (InvalidOperationException) { }
        }
    }
    private void Window_Closed(object? sender, EventArgs e)
    {
        _timer.Stop(); _noteDebounce.Stop();
        if (_record.Kind == "notes") NoteChanged?.Invoke(this, NotesText.Text);
        WidgetClosed?.Invoke(this, EventArgs.Empty);
    }
}
