using System.Text.Json.Serialization;

namespace AeroDesk.Models;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool UseAcrylic { get; set; } = true;
    public bool KeepWidgetsOnTop { get; set; } = false;
    public bool RestoreWidgetsOnLaunch { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public double WidgetOpacity { get; set; } = 0.88;
    public string AccentColor { get; set; } = "#FF87CEFA";
    public List<WidgetRecord> Widgets { get; set; } = [];
}

public sealed class WidgetRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Kind { get; set; } = "clock";
    public double Left { get; set; } = 80;
    public double Top { get; set; } = 80;
    public double Width { get; set; } = 260;
    public double Height { get; set; } = 160;
    public string NoteText { get; set; } = "";
    public bool Visible { get; set; } = true;
}
