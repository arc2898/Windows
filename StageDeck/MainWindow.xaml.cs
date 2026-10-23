using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StageDeck;

public partial class MainWindow : System.Windows.Window
{
    private static readonly string[] StageColors = ["#8B7CFF", "#E88CB9", "#68B9AD", "#E9A75A", "#68A8E8", "#D77C6B"];
    private readonly StageDeckData _data;
    private readonly List<(PreviewOverlayWindow Overlay, FrameworkElement Surface)> _previews = new();
    private readonly Dictionary<IntPtr, bool> _minimizedByStageDeck = new();
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private List<WindowSnapshot> _catalog = new();
    private StageGroup _selected;
    private StageGroup? _activeStage;
    private HwndSource? _hwndSource;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _isInitialized;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
        _data = StageStore.Load();
        if (_data.Stages.Count == 0) _data.Stages.Add(new StageGroup { Name = "Focus" });
        _selected = _data.Stages.FirstOrDefault(s => s.Id == _data.SelectedStageId) ?? _data.Stages[0];
        _catalog = WindowCatalog.Enumerate();
        _refreshTimer.Tick += (_, _) => RefreshShelf();
        SetupTrayIcon();
        SourceInitialized += RegisterGlobalHotkey;
        _isInitialized = true;
        Render();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdatePreviewPositions();
        UpdateHeaderStatus();
        PlaceShelves();
        _refreshTimer.Start();

    }

    private void PlaceShelves()
    {
        var area = SystemParameters.WorkArea;
        Width = 214;
        Height = area.Height;
        Left = area.Left;
        Top = area.Top;
        WindowState = WindowState.Normal;
        Topmost = true;
    }

    private void RefreshShelf()
    {
        if (!IsVisible) return;
        var fresh = WindowCatalog.Enumerate();
        var current = fresh.Where(w => w.IsMinimized).Select(w => w.Handle).Concat(ResolveHandles(_selected, fresh).Select(w => w.Handle)).ToHashSet();
        var previous = _catalog.Where(w => w.IsMinimized).Select(w => w.Handle).Concat(ResolveHandles(_selected, _catalog).Select(w => w.Handle)).ToHashSet();
        _catalog = fresh;
        if (!current.SetEquals(previous)) Render();
        else Dispatcher.BeginInvoke(UpdatePreviewPositions, DispatcherPriority.Loaded);
    }

    private void ShowStages_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var stage in _data.Stages)
        {
            var item = new MenuItem { Header = stage.Name, IsChecked = ReferenceEquals(stage, _selected), Tag = stage };
            item.Click += StageRow_Click;
            menu.Items.Add(item);
        }
        var add = new MenuItem { Header = "New stage…" };
        add.Click += NewStage_Click;
        menu.Items.Add(new Separator()); menu.Items.Add(add);
        menu.PlacementTarget = LeftRail; menu.IsOpen = true;
    }

    private void SetupTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Show window shelves  ·  Ctrl+Alt+Space", null, (_, _) => Dispatcher.BeginInvoke(ShowDashboard));
        menu.Items.Add("Restore windows", null, (_, _) => Dispatcher.BeginInvoke(RestoreAllManagedWindows));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit StageDeck", null, (_, _) => Dispatcher.BeginInvoke(ExitApplication));
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "StageDeck window shelves · Ctrl+Alt+Space",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.BeginInvoke(ShowDashboard);
    }

    private void RegisterGlobalHotkey(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(HotkeyWindowProc);
        var registered = NativeMethods.RegisterHotKey(handle, NativeMethods.HotkeyId,
            NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat, 0x20);
        if (!registered)
        {
            ShelfCount.Text = "Ctrl+Alt+Space is already in use. You can still open StageDeck from the notification area.";
        }
    }

    private IntPtr HotkeyWindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.ShowDashboardMessage)
        {
            ShowDashboard();
            handled = true;
        }
        else if (message == NativeMethods.WmHotkey && wParam.ToInt32() == NativeMethods.HotkeyId)
        {
            if (IsVisible) HideToTray(); else ShowDashboard();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void Render()
    {
        ClosePreviewOverlays();
        if (_selected is null) return;
        Heading.Text = _selected.Name;
        var openCount = CountOpenWindows(_selected);
        ShelfCount.Text = $"{openCount} windows · live previews";
        WindowCanvas.Children.Clear();
        var entries = _selected.Windows.ToList();
        var assignedEntries = _data.Stages.SelectMany(s => s.Windows).ToList();
        foreach (var minimized in _catalog.Where(w => w.IsMinimized))
        {
            if (assignedEntries.Any(e => SameWindow(e, minimized))) continue;
            entries.Add(minimized.ToEntry());
        }
        if (entries.Count != 0)
        {
            var reserved = new HashSet<IntPtr>();
            foreach (var entry in entries)
            {
                var match = WindowCatalog.Resolve(entry, _catalog, reserved);
                if (match is not null) reserved.Add(match.Handle);
                WindowCanvas.Children.Add(BuildWindowCard(entry, match));
            }
        }
        Dispatcher.BeginInvoke(UpdatePreviewPositions, DispatcherPriority.Loaded);
    }

    private Border BuildWindowCard(StageWindowEntry entry, WindowSnapshot? match)
    {
        var card = new Border
        {
            Style = (Style)FindResource("ShelfCard"), Width = 166, Height = 132,
            Margin = new Thickness(0, 0, 12, 12), Tag = entry,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 7, Opacity = .26 }
        };
        var content = new DockPanel();
        var titlebar = new Border { Background = Brushes.Transparent, Padding = new Thickness(3, 1, 0, 5) };
        DockPanel.SetDock(titlebar, Dock.Top);
        var top = new Grid(); top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); top.ColumnDefinitions.Add(new ColumnDefinition()); top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel { Margin = new Thickness(2, 0, 3, 0), VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock { Text = entry.Application, Foreground = "#F1F1F3".ToBrush(), FontSize = 10, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        title.Children.Add(new TextBlock { Text = entry.Title, Foreground = "#AAAAB1".ToBrush(), FontSize = 9, TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(title, 1); top.Children.Add(title);
        if (_selected.Windows.Contains(entry))
        {
            var remove = new Button { Content = "×", Background = Brushes.Transparent, Foreground = "#A9A9B0".ToBrush(), Padding = new Thickness(5, 1, 5, 1), FontSize = 14, ToolTip = "Remove from stage", Tag = entry };
            remove.Click += RemoveWindow_Click; Grid.SetColumn(remove, 2); top.Children.Add(remove);
        }
        titlebar.Child = top; content.Children.Add(titlebar);

        var surface = new Border { Height = 82, Background = "#17171C".ToBrush(), ClipToBounds = true, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 0, 1) };
        content.Children.Add(surface);
        var footer = new TextBlock
        {
            Text = match is null ? "Window closed" : (match.IsMinimized ? "MINIMIZED · LIVE" : "LIVE · Click to restore"),
            Foreground = match is null ? "#E9A75A".ToBrush() : "#85858E".ToBrush(),
            FontSize = 8, Margin = new Thickness(4, 5, 3, 1), TextTrimming = TextTrimming.CharacterEllipsis
        };
        DockPanel.SetDock(footer, Dock.Bottom); content.Children.Add(footer);
        card.Child = content;

        if (match is not null)
        {
            var ownerStage = _selected;
            var preview = new PreviewOverlayWindow(this, match.Handle, () => ActivateWindow(ownerStage, match.Handle));
            _previews.Add((preview, surface));
        }
        return card;
    }

    private static string ShortApp(string app) => app.Length > 2 ? app[..2].ToUpperInvariant() : app.ToUpperInvariant();

    private void UpdatePreviewPositions()
    {
        if (!IsVisible || PresentationSource.FromVisual(this)?.CompositionTarget is not { } target) return;
        var viewport = new Rect(0, 0, PreviewScroll.ViewportWidth, PreviewScroll.ViewportHeight);
        foreach (var pair in _previews)
        {
            var surface = pair.Surface;
            if (!surface.IsLoaded || surface.ActualWidth <= 0 || surface.ActualHeight <= 0) { pair.Overlay.HidePreview(); continue; }
            try
            {
                var relative = surface.TransformToAncestor(PreviewScroll).TransformBounds(new Rect(new Point(0, 0), new Size(surface.ActualWidth, surface.ActualHeight)));
                if (!viewport.Contains(relative)) { pair.Overlay.HidePreview(); continue; }
                var physical = surface.PointToScreen(new Point(0, 0));
                var logical = target.TransformFromDevice.Transform(physical);
                pair.Overlay.MoveOver(logical.X, logical.Y, surface.ActualWidth, surface.ActualHeight);
            }
            catch (InvalidOperationException) { pair.Overlay.HidePreview(); }
        }
    }

    private void ClosePreviewOverlays()
    {
        foreach (var pair in _previews.ToArray())
        {
            try { pair.Overlay.Close(); } catch (InvalidOperationException) { }
        }
        _previews.Clear();
    }

    private int CountOpenWindows(StageGroup stage)
    {
        var used = new HashSet<IntPtr>();
        return stage.Windows.Count(entry =>
        {
            var match = WindowCatalog.Resolve(entry, _catalog, used);
            if (match is null) return false;
            used.Add(match.Handle);
            return true;
        });
    }

    private void StageRow_Click(object sender, RoutedEventArgs e)
    {
        var stage = (sender as FrameworkElement)?.Tag as StageGroup ?? (sender as MenuItem)?.Tag as StageGroup;
        if (stage is not null)
        {
            _selected = stage;
            _data.SelectedStageId = stage.Id;
            SaveData();
            Render();
        }
    }

    private void NewStage_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptText("New stage", "Name this stage", "New stage");
        if (string.IsNullOrWhiteSpace(name)) return;
        var stage = new StageGroup { Name = name, Color = StageColors[_data.Stages.Count % StageColors.Length] };
        _data.Stages.Add(stage); _selected = stage; _data.SelectedStageId = stage.Id;
        SaveData(); Render(); ShelfCount.Text = $"Created '{stage.Name}'. Add currently open windows to it.";
    }

    private void AddWindows_Click(object sender, RoutedEventArgs e)
    {
        _catalog = WindowCatalog.Enumerate();
        if (_catalog.Count == 0) { ShelfCount.Text = "No open application windows found. Open an app, then refresh."; return; }
        var picker = new WindowPickerDialog(_catalog, _selected.Name) { Owner = this };
        if (picker.ShowDialog() != true) return;
        var added = 0;
        foreach (var window in picker.SelectedWindows)
        {
            foreach (var stage in _data.Stages) stage.Windows.RemoveAll(item => SameWindow(item, window));
            _selected.Windows.Add(window.ToEntry());
            added++;
        }
        SaveData(); Render();
        ShelfCount.Text = added == 0 ? "No windows selected." : $"Added {added} window{(added == 1 ? "" : "s")} to '{_selected.Name}'.";
    }

    private static bool SameWindow(StageWindowEntry entry, WindowSnapshot window)
    {
        var samePath = string.IsNullOrWhiteSpace(entry.ExecutablePath) || string.IsNullOrWhiteSpace(window.ExecutablePath)
            ? string.Equals(entry.Application, window.Application, StringComparison.OrdinalIgnoreCase)
            : string.Equals(entry.ExecutablePath, window.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        return samePath && string.Equals(entry.Title, window.Title, StringComparison.OrdinalIgnoreCase);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _catalog = WindowCatalog.Enumerate();
        Render();
        ShelfCount.Text = $"Found {_catalog.Count} open windows. Stage assignments are up to date.";
    }

    private void RemoveWindow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: StageWindowEntry entry })
        {
            var window = WindowCatalog.Resolve(entry, _catalog);
            if (window is not null && _minimizedByStageDeck.Remove(window.Handle, out var wasMaximized) && NativeMethods.IsWindow(window.Handle) && NativeMethods.IsIconic(window.Handle))
                NativeMethods.ShowWindow(window.Handle, wasMaximized ? NativeMethods.SwShowMaximized : NativeMethods.SwRestore);
            _selected.Windows.Remove(entry);
            SaveData(); Render(); ShelfCount.Text = "Removed '" + entry.Title + "' from this stage.";
        }
    }

    private void StageMenu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        var rename = new MenuItem { Header = "Rename stage" }; rename.Click += (_, _) => RenameSelectedStage();
        var delete = new MenuItem { Header = "Delete stage", IsEnabled = _data.Stages.Count > 1 }; delete.Click += (_, _) => DeleteSelectedStage();
        menu.Items.Add(rename); menu.Items.Add(delete); menu.PlacementTarget = (UIElement)sender; menu.IsOpen = true;
    }

    private void RenameSelectedStage()
    {
        var name = PromptText("Rename stage", "Stage name", _selected.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        _selected.Name = name; SaveData(); Render();
    }

    private void DeleteSelectedStage()
    {
        if (_data.Stages.Count <= 1) return;
        if (MessageBox.Show(this, $"Delete '{_selected.Name}' and remove its window assignments?", "Delete stage", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        RestoreStageWindows(_selected);
        _data.Stages.Remove(_selected);
        if (ReferenceEquals(_activeStage, _selected)) _activeStage = null;
        _selected = _data.Stages[0]; _data.SelectedStageId = _selected.Id;
        SaveData(); Render(); ShelfCount.Text = "Stage deleted. Its windows have been restored if StageDeck minimized them.";
    }

    private void SwitchStage_Click(object sender, RoutedEventArgs e) => SwitchToStage(_selected, IntPtr.Zero);

    private void SwitchToStage(StageGroup stage, IntPtr preferredWindow)
    {
        _catalog = WindowCatalog.Enumerate();
        var targetHandles = ResolveHandles(stage, _catalog);
        if (targetHandles.Count == 0)
        {
            ShelfCount.Text = "No assigned windows are open. Add or refresh windows before switching to this stage.";
            Render();
            return;
        }

        var targetSet = targetHandles.Select(w => w.Handle).ToHashSet();
        if (_data.IsEnabled)
        {
            foreach (var otherStage in _data.Stages.Where(s => !ReferenceEquals(s, stage)))
            {
                foreach (var window in ResolveHandles(otherStage, _catalog))
                {
                    if (targetSet.Contains(window.Handle) || NativeMethods.IsIconic(window.Handle)) continue;
                    _minimizedByStageDeck.TryAdd(window.Handle, NativeMethods.IsZoomed(window.Handle));
                    NativeMethods.ShowWindow(window.Handle, NativeMethods.SwMinimize);
                }
            }
            _activeStage = stage;
        }
        _selected = stage;
        _data.SelectedStageId = stage.Id;
        SaveData();
        var foreground = IntPtr.Zero;
        foreach (var window in targetHandles)
        {
            if (!NativeMethods.IsWindow(window.Handle)) continue;
            var wasMinimizedByUs = _minimizedByStageDeck.TryGetValue(window.Handle, out var wasMaximized);
            var shouldBeMaximized = wasMinimizedByUs ? wasMaximized : window.IsMaximized;
            if (NativeMethods.IsIconic(window.Handle))
                NativeMethods.ShowWindow(window.Handle, shouldBeMaximized ? NativeMethods.SwShowMaximized : NativeMethods.SwShowNoActivate);
            else
                NativeMethods.ShowWindow(window.Handle, shouldBeMaximized ? NativeMethods.SwShowMaximized : NativeMethods.SwShowNoActivate);
            _minimizedByStageDeck.Remove(window.Handle);
            if (window.Handle == preferredWindow) foreground = window.Handle;
            else if (foreground == IntPtr.Zero) foreground = window.Handle;
        }
        if (foreground != IntPtr.Zero) NativeMethods.SetForegroundWindow(foreground);
        UpdateTrayTooltip($"StageDeck · Active: {stage.Name} · Ctrl+Alt+Space to switch");
    }

    private static List<WindowSnapshot> ResolveHandles(StageGroup stage, IEnumerable<WindowSnapshot> windows)
    {
        var result = new List<WindowSnapshot>();
        var used = new HashSet<IntPtr>();
        foreach (var entry in stage.Windows)
        {
            var found = WindowCatalog.Resolve(entry, windows, used);
            if (found is not null) { result.Add(found); used.Add(found.Handle); }
        }
        return result;
    }

    private void ActivateWindow(StageGroup stage, IntPtr hwnd)
    {
        if (!NativeMethods.IsWindow(hwnd)) { ShelfCount.Text = "That window has closed. Refresh the stage to update its list."; return; }
        if (_data.IsEnabled)
        {
            SwitchToStage(stage, hwnd);
            return;
        }

        _selected = stage;
        _data.SelectedStageId = stage.Id;
        SaveData();
        NativeMethods.ShowWindow(hwnd, NativeMethods.IsZoomed(hwnd) ? NativeMethods.SwShowMaximized : NativeMethods.SwRestore);
        NativeMethods.SetForegroundWindow(hwnd);
    }

    private void RestoreStageWindows(StageGroup stage)
    {
        var windows = WindowCatalog.Enumerate();
        foreach (var window in ResolveHandles(stage, windows))
        {
            if (!_minimizedByStageDeck.Remove(window.Handle, out var wasMaximized)) continue;
            if (NativeMethods.IsWindow(window.Handle) && NativeMethods.IsIconic(window.Handle))
                NativeMethods.ShowWindow(window.Handle, wasMaximized ? NativeMethods.SwShowMaximized : NativeMethods.SwRestore);
        }
    }

    private void RestoreAllManagedWindows()
    {
        foreach (var (handle, wasMaximized) in _minimizedByStageDeck.ToArray())
        {
            if (NativeMethods.IsWindow(handle) && NativeMethods.IsIconic(handle))
                NativeMethods.ShowWindow(handle, wasMaximized ? NativeMethods.SwShowMaximized : NativeMethods.SwRestore);
        }
        _minimizedByStageDeck.Clear();
        _activeStage = null;
        if (_isInitialized && !_isExiting) { Render(); ShelfCount.Text = "Restored all windows that StageDeck had minimized."; }
        UpdateTrayTooltip("StageDeck · Ctrl+Alt+Space to hide shelves");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var enableStartup = new CheckBox { Content = "Start StageDeck when I sign in", IsChecked = IsStartupEnabled(), Margin = new Thickness(0, 0, 0, 15) };
        var hotkey = new TextBlock { Text = "Global dashboard shortcut: Ctrl + Alt + Space", Margin = new Thickness(0, 0, 0, 10) };
        var info = new TextBlock { Text = "Stage assignments are saved in your local AppData folder. Switching stages minimizes the other assigned windows; unassigned apps are left alone.", TextWrapping = TextWrapping.Wrap, MaxWidth = 360, Foreground = "#57575E".ToBrush() };
        var content = new StackPanel { Margin = new Thickness(18) };
        content.Children.Add(enableStartup); content.Children.Add(hotkey); content.Children.Add(info);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
        var save = new Button { Content = "Save", IsDefault = true, MinWidth = 80 };
        buttons.Children.Add(cancel); buttons.Children.Add(save); content.Children.Add(buttons);
        var dialog = new System.Windows.Window { Title = "StageDeck settings", Owner = this, Content = content, SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Brushes.White };
        save.Click += (_, _) => dialog.DialogResult = true;
        if (dialog.ShowDialog() == true)
        {
            SetStartupEnabled(enableStartup.IsChecked == true);
            SaveData();
            ShelfCount.Text = enableStartup.IsChecked == true ? "Settings saved. StageDeck will start when you sign in." : "Settings saved.";
        }
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue("StageDeck") is string;
    }

    private static void SetStartupEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
            ?? Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (key is null) return;
        if (enabled)
        {
            var executable = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executable)) key.SetValue("StageDeck", $"\"{executable}\" --tray");
        }
        else key.DeleteValue("StageDeck", false);
    }

    private string? PromptText(string title, string label, string initial)
    {
        var prompt = new TextBox { Text = initial, MinWidth = 280, Margin = new Thickness(0, 8, 0, 14), Padding = new Thickness(7) };
        var content = new StackPanel { Margin = new Thickness(18) };
        content.Children.Add(new TextBlock { Text = label }); content.Children.Add(prompt);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var save = new Button { Content = "OK", IsDefault = true, MinWidth = 80 };
        buttons.Children.Add(cancel); buttons.Children.Add(save); content.Children.Add(buttons);
        var dialog = new System.Windows.Window { Title = title, Owner = this, Content = content, SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Brushes.White };
        save.Click += (_, _) => dialog.DialogResult = true;
        return dialog.ShowDialog() == true ? prompt.Text.Trim() : null;
    }

    private void SaveData()
    {
        try
        {
            _data.SelectedStageId = _selected.Id;
            _data.IsEnabled = true;
            StageStore.Save(_data);
        }
        catch (Exception ex) { ShelfCount.Text = $"Could not save StageDeck data: {ex.Message}"; }
    }

    private void UpdateHeaderStatus() { }

    private void UpdateTrayTooltip(string text)
    {
        if (_trayIcon is not null) _trayIcon.Text = text.Length <= 63 ? text : text[..60] + "…";
    }

    private void ShowDashboard()
    {
        _catalog = WindowCatalog.Enumerate();
        Show(); PlaceShelves(); Render();
    }

    private void HideToTray()
    {
        ClosePreviewOverlays();
        Hide();
    }

    private void RestoreAll_Click(object? sender, EventArgs e) => Dispatcher.BeginInvoke(RestoreAllManagedWindows);

    private void Exit_Click(object sender, RoutedEventArgs e) => ExitApplication();

    private void ExitApplication()
    {
        _isExiting = true;
        RestoreAllManagedWindows();
        System.Windows.Application.Current.Shutdown();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, NativeMethods.HotkeyId);
        _hwndSource?.RemoveHook(HotkeyWindowProc);
        ClosePreviewOverlays();
        if (_trayIcon is not null) { var menu = _trayIcon.ContextMenuStrip; _trayIcon.Visible = false; _trayIcon.Dispose(); menu?.Dispose(); _trayIcon = null; }
    }

    private void Window_LocationChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(UpdatePreviewPositions, DispatcherPriority.Loaded);
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => Dispatcher.BeginInvoke(UpdatePreviewPositions, DispatcherPriority.Loaded);
    private void PreviewScroll_ScrollChanged(object sender, ScrollChangedEventArgs e) => Dispatcher.BeginInvoke(UpdatePreviewPositions, DispatcherPriority.Loaded);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => HideToTray();
}

internal static class BrushExtensions
{
    public static SolidColorBrush ToBrush(this string color) => new((Color)ColorConverter.ConvertFromString(color));
}











