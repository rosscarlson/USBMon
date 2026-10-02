using Microsoft.Win32;

namespace USBMon.Settings;

/// <summary>Per-user "run at startup" via HKCU\...\Run, requiring no elevation.</summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "USBMon";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                         ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            string exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Could not determine the running executable's path.");
            key.SetValue(ValueName, $"\"{exePath}\"", RegistryValueKind.String);
        }
        else
        {
            if (key.GetValue(ValueName) != null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
    }
}
