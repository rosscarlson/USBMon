using System.Windows.Forms;
using USBMon.Settings;
using USBMon.Theming;

namespace USBMon;

/// <summary>
/// WPF has no built-in tray icon, so this wraps System.Windows.Forms.NotifyIcon —
/// the standard, well-supported way to put an icon in the tray from a WPF app.
/// </summary>
internal sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _themeLightItem;
    private readonly ToolStripMenuItem _themeDarkItem;
    private readonly ToolStripMenuItem _themeSystemItem;

    public event Action? OpenRequested;
    public event Action<ThemeMode>? ThemeRequested;
    public event Action? CheckUpdatesRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        var openItem = new ToolStripMenuItem("Open", null, (_, _) => OpenRequested?.Invoke());
        _startupItem = new ToolStripMenuItem("Run at startup") { CheckOnClick = true, Checked = StartupManager.IsEnabled() };
        _startupItem.Click += (_, _) => StartupManager.SetEnabled(_startupItem.Checked);

        _themeLightItem = new ToolStripMenuItem("Light", null, (_, _) => ThemeRequested?.Invoke(ThemeMode.Light));
        _themeDarkItem = new ToolStripMenuItem("Dark", null, (_, _) => ThemeRequested?.Invoke(ThemeMode.Dark));
        _themeSystemItem = new ToolStripMenuItem("System", null, (_, _) => ThemeRequested?.Invoke(ThemeMode.System));
        var themeMenu = new ToolStripMenuItem("Theme");
        themeMenu.DropDownItems.Add(_themeLightItem);
        themeMenu.DropDownItems.Add(_themeDarkItem);
        themeMenu.DropDownItems.Add(_themeSystemItem);

        var checkUpdatesItem = new ToolStripMenuItem("Check for updates", null, (_, _) => CheckUpdatesRequested?.Invoke());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitRequested?.Invoke());

        var menu = new ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(themeMenu);
        menu.Items.Add(checkUpdatesItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = new NotifyIcon
        {
            Icon = AppIcon.Create(),
            Text = "USB Mon",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => OpenRequested?.Invoke();

        UpdateThemeChecks(ThemeManager.Choice);
    }

    public void UpdateThemeChecks(ThemeMode mode)
    {
        _themeLightItem.Checked = mode == ThemeMode.Light;
        _themeDarkItem.Checked = mode == ThemeMode.Dark;
        _themeSystemItem.Checked = mode == ThemeMode.System;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
