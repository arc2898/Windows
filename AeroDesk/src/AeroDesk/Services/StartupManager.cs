using Microsoft.Win32;

namespace AeroDesk.Services;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AeroDesk";

    public static bool IsEnabled()
    {
        try { using var key = Registry.CurrentUser.OpenSubKey(RunKey); return key?.GetValue(AppName) is string; }
        catch { return false; }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Unable to determine the current executable path.");
            key.SetValue(AppName, $"\"{exe}\"");
        }
        else key.DeleteValue(AppName, false);
    }
}
