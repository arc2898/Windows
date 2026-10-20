using System.Windows;
using System.Drawing;
using AeroDesk.Models;
using AeroDesk.Services;
using WinForms = System.Windows.Forms;

namespace AeroDesk;

public partial class App : Application
{
    private WinForms.NotifyIcon? _trayIcon;
    public static SettingsStore Settings { get; } = new();
    public static AppSettings CurrentSettings { get; private set; } = new();
    public static WidgetManager? Widgets { get; private set; }
    public static bool IsShuttingDown { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CurrentSettings = Settings.Load();
        Widgets = new WidgetManager(Settings, CurrentSettings);
        InitializeTrayIcon();
        var dashboard = new MainWindow();
        MainWindow = dashboard;
        dashboard.Show();
        if (CurrentSettings.RestoreWidgetsOnLaunch) Widgets.RestoreSavedWidgets();
    }

    private void InitializeTrayIcon()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Open AeroDesk", null, (_, _) => ShowDashboard());
        menu.Items.Add("Add clock widget", null, (_, _) => Widgets?.AddWidget("clock"));
        menu.Items.Add("Add notes widget", null, (_, _) => Widgets?.AddWidget("notes"));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Quit AeroDesk", null, (_, _) => RequestShutdown());
        _trayIcon = new WinForms.NotifyIcon
        {
            Text = "AeroDesk — Glass desktop widgets",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowDashboard();
    }

    public static void ShowDashboard()
    {
        if (Current.MainWindow is not Window window) return;
        if (!window.IsVisible) window.Show();
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Activate();
    }

    public static void RequestShutdown()
    {
        IsShuttingDown = true;
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon is not null) { _trayIcon.Visible = false; _trayIcon.Dispose(); _trayIcon = null; }
        Widgets?.Dispose();
        base.OnExit(e);
    }
}
