using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Threading;
using AeroDesk.Models;
using AeroDesk.Services;

namespace AeroDesk;

public partial class MainWindow : Window
{
    private bool _initializing = true;
    private readonly string[] _kinds = ["clock", "calendar", "notes", "system", "network", "battery"];
    private readonly DispatcherTimer _opacitySaveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

    public MainWindow()
    {
        InitializeComponent();
        _opacitySaveTimer.Tick += (_, _) => { _opacitySaveTimer.Stop(); App.Settings.Save(App.CurrentSettings); };
        foreach (var kind in _kinds)
        {
            var button = new Button { Padding = new Thickness(12), Margin = new Thickness(4), MinHeight = 74, HorizontalContentAlignment = HorizontalAlignment.Left };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = IconFor(kind), FontSize = 22, Foreground = (System.Windows.Media.Brush)FindResource("GlassAccent") });
            stack.Children.Add(new TextBlock { Text = WidgetKinds.Label(kind), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 0) });
            button.Content = stack;
            button.Click += (_, _) => App.Widgets?.AddWidget(kind);
            WidgetGrid.Children.Add(button);
        }
    }

    private static string IconFor(string kind) => kind switch
    { "clock" => "◷", "calendar" => "▦", "notes" => "▤", "system" => "◉", "network" => "↗", "battery" => "▰", _ => "◇" };

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var settings = App.CurrentSettings;
        _initializing = true;
        AcrylicCheck.IsChecked = settings.UseAcrylic;
        TopmostCheck.IsChecked = settings.KeepWidgetsOnTop;
        RestoreCheck.IsChecked = settings.RestoreWidgetsOnLaunch;
        StartupCheck.IsChecked = StartupManager.IsEnabled();
        OpacitySlider.Value = settings.WidgetOpacity;
        _initializing = false;
        GlassEffect.Apply(this, settings.UseAcrylic, 0xE8);
        RefreshStats();
    }

    private void Setting_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        var s = App.CurrentSettings;
        s.UseAcrylic = AcrylicCheck.IsChecked == true;
        s.KeepWidgetsOnTop = TopmostCheck.IsChecked == true;
        s.RestoreWidgetsOnLaunch = RestoreCheck.IsChecked == true;
        App.Settings.Save(s);
        App.Widgets?.ApplySettings();
        GlassEffect.Apply(this, s.UseAcrylic, 0xE8);
    }

    private void Startup_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        try
        {
            var enabled = StartupCheck.IsChecked == true;
            StartupManager.SetEnabled(enabled);
            App.CurrentSettings.StartWithWindows = enabled;
            App.Settings.Save(App.CurrentSettings);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not update Windows startup: {ex.Message}", "AeroDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
            _initializing = true; StartupCheck.IsChecked = StartupManager.IsEnabled(); _initializing = false;
        }
    }

    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing || App.CurrentSettings is null) return;
        App.CurrentSettings.WidgetOpacity = OpacitySlider.Value;
        App.Widgets?.ApplySettings(persist: false);
        _opacitySaveTimer.Stop();
        _opacitySaveTimer.Start();
    }

    public void RefreshStats() => StatsText.Text = $"{App.Widgets?.ActiveCount ?? 0} widgets active  •  Settings stored in %APPDATA%\\AeroDesk";
    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", App.Settings.RootDirectory) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "AeroDesk"); }
    }
    private void Quit_Click(object sender, RoutedEventArgs e) => App.RequestShutdown();
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (App.IsShuttingDown)
        {
            _opacitySaveTimer.Stop();
            try { App.Settings.Save(App.CurrentSettings); } catch { }
            return;
        }
        e.Cancel = true;
        Hide();
    }
}
