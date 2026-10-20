using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AeroDesk.Services;

/// <summary>Uses the documented Windows 11 system backdrop first and best-effort acrylic as fallback.</summary>
public static class GlassEffect
{
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int AccentPolicyAttribute = 19;
    private const int AccentEnableAcrylicBlur = 4;
    [StructLayout(LayoutKind.Sequential)] private struct AccentPolicy { public int State, Flags, GradientColor, AnimationId; }
    [StructLayout(LayoutKind.Sequential)] private struct WindowCompositionAttributeData { public int Attribute; public IntPtr Data; public int SizeOfData; }
    [DllImport("dwmapi.dll", PreserveSig = true)] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static void Apply(Window window, bool acrylic, byte opacity = 0xD8, byte red = 22, byte green = 39, byte blue = 59)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int dark = 1; _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
            int backdrop = acrylic ? 3 : 2; // transient acrylic or main-window Mica backdrop
            _ = DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
            if (acrylic)
            {
                var policy = new AccentPolicy { State = AccentEnableAcrylicBlur, Flags = 2, GradientColor = (opacity << 24) | (blue << 16) | (green << 8) | red };
                var size = Marshal.SizeOf<AccentPolicy>(); var ptr = Marshal.AllocHGlobal(size);
                try { Marshal.StructureToPtr(policy, ptr, false); var data = new WindowCompositionAttributeData { Attribute = AccentPolicyAttribute, Data = ptr, SizeOfData = size }; _ = SetWindowCompositionAttribute(hwnd, ref data); }
                finally { Marshal.FreeHGlobal(ptr); }
            }
        }
        catch { /* Effect is optional; opaque styling remains functional. */ }
    }
}
