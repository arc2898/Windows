using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace AeroDesk.Services;

public static class SystemMetrics
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatus
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatus>();
        public uint MemoryLoad; public ulong TotalPhys; public ulong AvailPhys;
        public ulong TotalPageFile; public ulong AvailPageFile; public ulong TotalVirtual; public ulong AvailVirtual; public ulong AvailExtendedVirtual;
    }
    [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus status);

    public static (double UsedPercent, string Used, string Total) Memory()
    {
        var status = new MemoryStatus();
        if (!GlobalMemoryStatusEx(status) || status.TotalPhys == 0) return (0, "—", "—");
        var used = status.TotalPhys - status.AvailPhys;
        return (status.MemoryLoad, FormatBytes((long)Math.Min(used, (ulong)long.MaxValue)), FormatBytes((long)Math.Min(status.TotalPhys, (ulong)long.MaxValue)));
    }

    public static (long Received, long Sent) NetworkTotals()
    {
        long rx = 0, tx = 0;
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            try { var s = adapter.GetIPv4Statistics(); rx += s.BytesReceived; tx += s.BytesSent; } catch { }
        }
        return (rx, tx);
    }

    public static (bool Present, bool Charging, int Percent) Battery()
    {
        if (!GetSystemPowerStatus(out var s)) return (false, false, -1);
        var present = s.BatteryFlag != 128 && s.BatteryLifePercent != 255;
        return (present, (s.BatteryFlag & 8) != 0, s.BatteryLifePercent);
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double n = Math.Max(0, bytes); var unit = 0;
        while (n >= 1024 && unit < units.Length - 1) { n /= 1024; unit++; }
        return $"{n:0.0} {units[unit]}";
    }

    [StructLayout(LayoutKind.Sequential)] private struct PowerStatus
    {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetSystemPowerStatus(out PowerStatus status);
}
