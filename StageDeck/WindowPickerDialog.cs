using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StageDeck;

internal sealed class WindowPickerDialog : System.Windows.Window
{
    private readonly System.Windows.Controls.ListBox _list;
    public IReadOnlyList<WindowSnapshot> SelectedWindows { get; private set; } = Array.Empty<WindowSnapshot>();

    public WindowPickerDialog(IEnumerable<WindowSnapshot> windows, string stageName)
    {
        Title = $"Add windows to {stageName}";
        Width = 700;
        Height = 520;
        MinWidth = 540;
        MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        Background = System.Windows.Media.Brushes.White;
        var root = new DockPanel { Margin = new Thickness(18) };
        var intro = new TextBlock { Text = "Choose one or more currently open windows. A window can belong to only one stage.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12), FontSize = 13 };
        DockPanel.SetDock(intro, Dock.Top);
        root.Children.Add(intro);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(buttons, Dock.Bottom);
        var cancel = new Button { Content = "Cancel", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
        var add = new Button { Content = "Add selected", MinWidth = 110, IsDefault = true };
        add.Click += (_, _) => Accept();
        buttons.Children.Add(cancel); buttons.Children.Add(add); root.Children.Add(buttons);
        _list = new System.Windows.Controls.ListBox { SelectionMode = SelectionMode.Extended, ItemsSource = windows, DisplayMemberPath = nameof(WindowSnapshot.DisplayText), FontSize = 12 };
        _list.MouseDoubleClick += (_, _) => Accept();
        root.Children.Add(_list);
        Content = root;
    }

    private void Accept()
    {
        SelectedWindows = _list.SelectedItems.Cast<WindowSnapshot>().ToList();
        if (SelectedWindows.Count > 0) DialogResult = true;
    }
}

