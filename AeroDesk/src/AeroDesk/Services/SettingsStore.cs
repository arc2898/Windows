using System.IO;
using System.Text.Json;
using AeroDesk.Models;

namespace AeroDesk.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string RootDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AeroDesk");
    public string SettingsPath => Path.Combine(RootDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions) ?? new AppSettings();
            settings.Widgets ??= [];
            settings.Widgets.RemoveAll(w => w.Id == Guid.Empty || !WidgetKinds.IsValid(w.Kind));
            settings.WidgetOpacity = Math.Clamp(settings.WidgetOpacity, 0.35, 1.0);
            return settings;
        }
        catch
        {
            // A malformed file must not prevent startup. Keep the original for recovery.
            try { File.Copy(SettingsPath, SettingsPath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), false); } catch { }
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(RootDirectory);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
        if (File.Exists(SettingsPath))
        {
            var backup = SettingsPath + ".bak";
            try { File.Replace(temp, SettingsPath, backup, true); }
            catch { File.Copy(temp, SettingsPath, true); File.Delete(temp); }
        }
        else File.Move(temp, SettingsPath);
    }
}

public static class WidgetKinds
{
    public static readonly string[] All = ["clock", "calendar", "notes", "system", "network", "battery"];
    public static bool IsValid(string kind) => All.Contains(kind, StringComparer.OrdinalIgnoreCase);
    public static string Label(string kind) => kind.ToLowerInvariant() switch
    {
        "clock" => "Clock", "calendar" => "Date & calendar", "notes" => "Quick notes",
        "system" => "System monitor", "network" => "Network traffic", "battery" => "Battery status", _ => kind
    };
}
