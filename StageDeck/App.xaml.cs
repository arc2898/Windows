using System.Threading;
using System.Windows;

namespace StageDeck;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, @"Local\StageDeck_Arc2898_StageManager", out _ownsMutex);
        base.OnStartup(e);
        if (!_ownsMutex)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var existingWindow = NativeMethods.FindWindow(null, "StageDeck");
                if (existingWindow != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(existingWindow, NativeMethods.ShowDashboardMessage, IntPtr.Zero, IntPtr.Zero);
                    break;
                }
                Thread.Sleep(100);
            }
            Shutdown();
            return;
        }

        var dashboard = new MainWindow();
        MainWindow = dashboard;
        dashboard.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex) _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
