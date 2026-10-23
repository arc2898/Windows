using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace StageDeck;

internal sealed class PreviewOverlayWindow : System.Windows.Window
{
    private readonly IntPtr _source;
    private readonly Action _activate;
    private IntPtr _thumbnail;
    private IntPtr _hwnd;
    private bool _closed;

    public PreviewOverlayWindow(System.Windows.Window owner, IntPtr source, Action activate)
    {
        Owner = owner;
        _source = source;
        _activate = activate;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Background = Brushes.Black;
        WindowStartupLocation = WindowStartupLocation.Manual;
        MouseLeftButtonDown += Preview_MouseLeftButtonDown;
        SourceInitialized += (_, _) => RegisterThumbnail();
        SizeChanged += (_, _) => UpdateThumbnail();
        Closed += (_, _) => UnregisterThumbnail();
    }

    public void MoveOver(double left, double top, double width, double height)
    {
        if (_closed) return;
        Left = left; Top = top; Width = Math.Max(1, width); Height = Math.Max(1, height);
        if (!IsVisible) Show();
        Dispatcher.BeginInvoke(new Action(UpdateThumbnail), DispatcherPriority.Loaded);
    }

    public void HidePreview()
    {
        if (IsVisible) Hide();
    }

    private void Preview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _activate();
        e.Handled = true;
    }

    private void RegisterThumbnail()
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        if (_source != IntPtr.Zero && NativeMethods.DwmRegisterThumbnail(_hwnd, _source, out _thumbnail) >= 0)
            UpdateThumbnail();
    }

    private void UpdateThumbnail()
    {
        if (_thumbnail == IntPtr.Zero || !NativeMethods.GetClientRect(_hwnd, out var rect)) return;
        var properties = new NativeMethods.DwmThumbnailProperties
        {
            Flags = 0x00000001 | 0x00000008,
            Destination = new NativeMethods.NativeRect { Left = 0, Top = 0, Right = Math.Max(1, rect.Right), Bottom = Math.Max(1, rect.Bottom) },
            Opacity = 255,
            Visible = true,
            SourceClientAreaOnly = false
        };
        NativeMethods.DwmUpdateThumbnailProperties(_thumbnail, ref properties);
    }

    private void UnregisterThumbnail()
    {
        _closed = true;
        if (_thumbnail != IntPtr.Zero) NativeMethods.DwmUnregisterThumbnail(_thumbnail);
        _thumbnail = IntPtr.Zero;
    }
}
