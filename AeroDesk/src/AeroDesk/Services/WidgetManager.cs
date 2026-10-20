using System.Windows;
using System.Windows.Threading;
using AeroDesk.Models;

namespace AeroDesk.Services;

public sealed class WidgetManager : IDisposable
{
    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private readonly Dictionary<Guid, WidgetWindow> _open = [];
    private bool _restoring;
    private bool _disposed;
    private readonly DispatcherTimer _layoutSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    public int ActiveCount => _open.Count;

    public WidgetManager(SettingsStore store, AppSettings settings)
    {
        _store = store; _settings = settings;
        _layoutSaveTimer.Tick += (_, _) => { _layoutSaveTimer.Stop(); Save(); };
    }

    public WidgetWindow AddWidget(string kind)
    {
        if (!WidgetKinds.IsValid(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var existing = _settings.Widgets.FirstOrDefault(w => w.Kind == kind && _open.ContainsKey(w.Id));
        if (existing is not null && _open.TryGetValue(existing.Id, out var alreadyOpen))
        {
            if (!alreadyOpen.IsVisible) alreadyOpen.Show();
            return alreadyOpen;
        }
        var record = new WidgetRecord { Kind = kind };
        record.Left = Math.Max(16, SystemParameters.WorkArea.Width - record.Width - 32);
        record.Top = 70 + _settings.Widgets.Count * 18 % 350;
        _settings.Widgets.Add(record);
        record.Visible = true;
        var window = Create(record);
        window.Show();
        Save();
        return window;
    }

    private WidgetWindow Create(WidgetRecord record)
    {
        var window = new WidgetWindow(record, _settings);
        _open[record.Id] = window;
        window.LocationChanged += (_, _) => { if (!_restoring) { SaveBounds(record, window); QueueLayoutSave(); } };
        window.SizeChanged += (_, _) => { if (!_restoring) { SaveBounds(record, window); QueueLayoutSave(); } };
        window.NoteChanged += (_, text) => { record.NoteText = text; Save(); };
        window.WidgetClosed += (_, _) =>
        {
            _open.Remove(record.Id);
            record.Visible = false;
            if (!_disposed) Save();
            (System.Windows.Application.Current.MainWindow as MainWindow)?.RefreshStats();
        };
        window.Loaded += (_, _) => (System.Windows.Application.Current.MainWindow as MainWindow)?.RefreshStats();
        return window;
    }

    public void RestoreSavedWidgets()
    {
        _restoring = true;
        try { foreach (var item in _settings.Widgets.Where(w => w.Visible).ToArray()) Create(item).Show(); }
        finally { _restoring = false; }
    }

    private static void SaveBounds(WidgetRecord record, WidgetWindow window)
    {
        record.Left = window.Left; record.Top = window.Top; record.Width = window.Width; record.Height = window.Height;
    }
    public void ApplySettings(bool persist = true)
    {
        foreach (var window in _open.Values.ToArray()) window.ApplySettings(_settings);
        if (persist) Save();
    }
    private void QueueLayoutSave()
    {
        if (_disposed || _restoring) return;
        _layoutSaveTimer.Stop();
        _layoutSaveTimer.Start();
    }
    private void Save() { try { _store.Save(_settings); } catch { /* transient disk errors should not crash the UI */ } }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _layoutSaveTimer.Stop();
        Save();
        foreach (var w in _open.Values.ToArray()) w.Close();
        _open.Clear();
    }
}
