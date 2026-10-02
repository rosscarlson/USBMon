using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using USBMon.Settings;

namespace USBMon.Theming;

/// <summary>Swaps the colour palette dictionary at runtime and keeps window title bars in sync.</summary>
internal static class ThemeManager
{
    private static bool _hooked;

    public static ThemeMode Choice { get; private set; } = ThemeMode.System;
    public static bool IsDark { get; private set; } = true;

    public static event Action? ThemeChanged;

    public static void Apply(ThemeMode choice)
    {
        Choice = choice;
        if (!_hooked)
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _hooked = true;
        }

        IsDark = choice switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            ThemeMode.System => SystemPrefersDark(),
            _ => true
        };

        var app = Application.Current;
        var palette = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/USBMon;component/Themes/{(IsDark ? "Dark" : "Light")}.xaml", UriKind.Absolute)
        };
        app.Resources.MergedDictionaries[0] = palette; // index 0 is always the palette (see App.xaml)

        foreach (Window w in app.Windows) ApplyTitleBar(w);

        ThemeChanged?.Invoke();
    }

    public static bool SystemPrefersDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch
        {
            return true;
        }
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (Choice == ThemeMode.System && e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Application.Current?.Dispatcher.BeginInvoke(() => Apply(ThemeMode.System));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void ApplyTitleBar(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;
        int dark = IsDark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hwnd, 19, ref dark, sizeof(int));
        }
    }
}
