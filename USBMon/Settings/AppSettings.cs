namespace USBMon.Settings;

public sealed class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public bool RunAtStartup { get; set; } = false;

    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowWidth { get; set; } = 1100;
    public int WindowHeight { get; set; } = 600;
    public bool WindowMaximized { get; set; } = false;

    public List<int> ColumnWidths { get; set; } = new();
    public int SortColumn { get; set; } = 0;
    public bool SortAscending { get; set; } = false;
}
