using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StageDeck;

internal static class NativeMethods
{
    internal const int SwRestore = 9;
    internal const int SwShow = 5;
    internal const int SwShowMaximized = 3;
    internal const int SwShowNoActivate = 4;
    internal const int GwlExStyle = -20;
    internal const int WsExToolWindow = 0x00000080;
    internal const int SwMinimize = 6;
    internal const int WmHotkey = 0x0312;
    internal const int ShowDashboardMessage = 0x8000 + 0x4D;
    internal const int ModControl = 0x0002;
    internal const int ModAlt = 0x0001;
    internal const int ModNoRepeat = 0x4000;
    internal const int HotkeyId = 0x5D31;

    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsZoomed(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr hwnd, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] internal static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")] internal static extern IntPtr FindWindow(string? className, string windowName);
    [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("dwmapi.dll")] internal static extern int DwmRegisterThumbnail(IntPtr destination, IntPtr source, out IntPtr thumbnail);
    [DllImport("dwmapi.dll")] internal static extern int DwmUnregisterThumbnail(IntPtr thumbnail);
    [DllImport("dwmapi.dll")] internal static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail, ref DwmThumbnailProperties properties);

    internal delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DwmThumbnailProperties
    {
        public uint Flags;
        public NativeRect Destination;
        public NativeRect Source;
        public byte Opacity;
        [MarshalAs(UnmanagedType.Bool)] public bool Visible;
        [MarshalAs(UnmanagedType.Bool)] public bool SourceClientAreaOnly;
    }
}

internal sealed record WindowSnapshot(IntPtr Handle, string Title, string Application, string ExecutablePath, bool IsMinimized, bool IsMaximized)
{
    public string DisplayText => $"{Application}   ·   {Title}";

    public StageWindowEntry ToEntry() => new()
    {
        Title = Title,
        Application = Application,
        ExecutablePath = ExecutablePath
    };
}

internal static class WindowCatalog
{
    public static List<WindowSnapshot> Enumerate()
    {
        var windows = new List<WindowSnapshot>();
        var ownProcessId = (uint)Environment.ProcessId;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd) || NativeMethods.GetWindow(hwnd, 4) != IntPtr.Zero ||
                (NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle) & NativeMethods.WsExToolWindow) != 0)
                return true;

            var length = NativeMethods.GetWindowTextLength(hwnd);
            if (length <= 0) return true;
            var title = new StringBuilder(Math.Min(length + 1, 2048));
            NativeMethods.GetWindowText(hwnd, title, title.Capacity);
            var caption = title.ToString().Trim();
            if (caption.Length == 0) return true;

            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == ownProcessId) return true;

            try
            {
                using var process = Process.GetProcessById((int)pid);
                var app = process.ProcessName;
                var executablePath = string.Empty;
                try { executablePath = process.MainModule?.FileName ?? string.Empty; }
                catch (Exception) { }
                windows.Add(new WindowSnapshot(hwnd, caption, app, executablePath, NativeMethods.IsIconic(hwnd), NativeMethods.IsZoomed(hwnd)));
            }
            catch (Exception) { }
            return true;
        }, IntPtr.Zero);

        return windows.OrderBy(w => w.Application, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public static WindowSnapshot? Resolve(StageWindowEntry entry, IEnumerable<WindowSnapshot> windows, ISet<IntPtr>? reserved = null)
    {
        var matching = windows.Where(w => reserved?.Contains(w.Handle) != true &&
            (string.IsNullOrWhiteSpace(entry.ExecutablePath) || string.IsNullOrWhiteSpace(w.ExecutablePath)
                ? string.Equals(w.Application, entry.Application, StringComparison.OrdinalIgnoreCase)
                : string.Equals(w.ExecutablePath, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase))).ToList();
        var exactTitle = matching.FirstOrDefault(w => string.Equals(w.Title, entry.Title, StringComparison.OrdinalIgnoreCase));
        if (exactTitle is not null) return exactTitle;
        return matching.Count == 1 ? matching[0] : null;
    }
}

internal sealed class StageWindowEntry
{
    public string Title { get; set; } = "";
    public string Application { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
}

internal sealed class StageGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#8B7CFF";
    public List<StageWindowEntry> Windows { get; set; } = new();
}

internal sealed class StageDeckData
{
    public List<StageGroup> Stages { get; set; } = new();
    public string SelectedStageId { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}

internal static class StageStore
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StageDeck");
    private static readonly string FilePath = Path.Combine(Folder, "stages.json");

    public static StageDeckData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<StageDeckData>(File.ReadAllText(FilePath));
                if (data?.Stages.Count > 0) return data;
            }
        }
        catch (Exception) { }
        var initial = new StageDeckData();
        initial.Stages.Add(new StageGroup { Name = "Focus", Color = "#8B7CFF" });
        initial.SelectedStageId = initial.Stages[0].Id;
        return initial;
    }

    public static void Save(StageDeckData data)
    {
        Directory.CreateDirectory(Folder);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, true);
    }
}






