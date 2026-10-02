using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using USBMon.Filtering;
using USBMon.Models;
using USBMon.Settings;
using USBMon.Theming;
using USBMon.Updates;
using USBMon.ViewModels;

namespace USBMon;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<DeviceRecordRow> _rows = new();
    private readonly List<DeviceRecord> _allRecords = new();
    private readonly List<IEventFilter> _activeFilters = new();
    private readonly AppSettings _settings;
    private bool _suppressThemeEvent;

    public bool AllowClose { get; set; }

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        EventGrid.ItemsSource = _rows;

        if (settings.WindowX >= 0 && settings.WindowY >= 0)
        {
            Left = settings.WindowX;
            Top = settings.WindowY;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        Width = Math.Max(settings.WindowWidth, MinWidth);
        Height = Math.Max(settings.WindowHeight, MinHeight);
        WindowState = settings.WindowMaximized ? WindowState.Maximized : WindowState.Normal;

        if (settings.ColumnWidths.Count == EventGrid.Columns.Count)
        {
            for (int i = 0; i < EventGrid.Columns.Count; i++)
            {
                EventGrid.Columns[i].Width = new DataGridLength(settings.ColumnWidths[i]);
            }
        }

        Loaded += (_, _) => ApplyInitialSort();

        _suppressThemeEvent = true;
        ThemeBox.SelectedIndex = settings.Theme switch
        {
            ThemeMode.Dark => 0,
            ThemeMode.Light => 1,
            _ => 2
        };
        _suppressThemeEvent = false;

        ThemeManager.ThemeChanged += OnExternalThemeChanged;
        Closed += (_, _) => ThemeManager.ThemeChanged -= OnExternalThemeChanged;
    }

    private void ApplyInitialSort()
    {
        if (EventGrid.Columns.Count == 0) return;
        int columnIndex = Math.Clamp(_settings.SortColumn, 0, EventGrid.Columns.Count - 1);
        var column = EventGrid.Columns[columnIndex];
        var direction = _settings.SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;

        var view = CollectionViewSource.GetDefaultView(_rows);
        view.SortDescriptions.Clear();
        if (column is DataGridBoundColumn bound && bound.Binding is System.Windows.Data.Binding b)
        {
            view.SortDescriptions.Add(new SortDescription(b.Path.Path, direction));
            column.SortDirection = direction;
        }
    }

    private void OnExternalThemeChanged()
    {
        _suppressThemeEvent = true;
        ThemeBox.SelectedIndex = ThemeManager.Choice switch
        {
            ThemeMode.Dark => 0,
            ThemeMode.Light => 1,
            _ => 2
        };
        _suppressThemeEvent = false;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) => ThemeManager.ApplyTitleBar(this);

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressThemeEvent) return;
        var mode = ThemeBox.SelectedIndex switch
        {
            0 => ThemeMode.Dark,
            1 => ThemeMode.Light,
            _ => ThemeMode.System
        };
        ((App)Application.Current).SetTheme(mode);
    }

    public void SetLogFilePath(string path) => LogPathText.Text = $"Logging to: {path}";

    public void AddRecord(DeviceRecord record)
    {
        _allRecords.Add(record);
        CountText.Text = $"{_allRecords.Count} event{(_allRecords.Count == 1 ? "" : "s")}";

        if (_activeFilters.Count == 0 || _activeFilters.All(f => f.Matches(record)))
        {
            _rows.Insert(0, new DeviceRecordRow(record));
        }
    }

    private void EventGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (EventGrid.SelectedItem is not DeviceRecordRow row) return;
        var details = new DeviceDetailsWindow(row.Record.Info) { Owner = this };
        details.ShowDialog();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        SaveWindowSettings();
    }

    public void SaveWindowSettings()
    {
        var settings = SettingsManager.Load();
        settings.Theme = ThemeManager.Choice;

        if (WindowState == WindowState.Normal)
        {
            settings.WindowX = (int)Left;
            settings.WindowY = (int)Top;
            settings.WindowWidth = (int)Width;
            settings.WindowHeight = (int)Height;
            settings.WindowMaximized = false;
        }
        else
        {
            settings.WindowMaximized = WindowState == WindowState.Maximized;
        }

        settings.ColumnWidths = EventGrid.Columns.Select(c => (int)c.ActualWidth).ToList();

        var sortedColumn = EventGrid.Columns.FirstOrDefault(c => c.SortDirection != null);
        if (sortedColumn != null)
        {
            settings.SortColumn = EventGrid.Columns.IndexOf(sortedColumn);
            settings.SortAscending = sortedColumn.SortDirection == ListSortDirection.Ascending;
        }

        SettingsManager.Save(settings);
    }

    // ---------------------------------------------------------------- updates

    private UpdateInfo? _pendingUpdate;
    private bool _updateBusy;

    /// <summary>Automatic checks stay silent unless an update exists; manual checks always report something.</summary>
    public async Task CheckForUpdatesAsync(bool manual)
    {
        if (_updateBusy) return;
        _updateBusy = true;
        try
        {
            var update = await UpdateService.CheckAsync();
            if (update != null) ShowUpdateAvailable(update);
            else if (manual) ShowUpdateMessage($"You're up to date (version {UpdateService.Display(UpdateService.CurrentVersion)}).");
        }
        catch (Exception ex)
        {
            if (manual) ShowUpdateMessage("Couldn't check for updates: " + ex.Message);
        }
        finally
        {
            _updateBusy = false;
        }
    }

    private void ShowUpdateAvailable(UpdateInfo update, string? note = null)
    {
        _pendingUpdate = update;
        UpdateText.Text = note ?? $"Version {UpdateService.Display(update.Version)} is available. You have {UpdateService.Display(UpdateService.CurrentVersion)}.";
        UpdateBanner.Visibility = Visibility.Visible;
    }

    private void ShowUpdateMessage(string text)
    {
        UpdateText.Text = text;
        UpdateBanner.Visibility = Visibility.Visible;
    }

    private async void UpdateInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is not { } update || _updateBusy) return;
        _updateBusy = true;
        UpdateInstallButton.IsEnabled = false;
        try
        {
            UpdateText.Text = "Downloading update…";
            var progress = new Progress<double>(f => UpdateText.Text = $"Downloading update… {f:P0}");
            string installer = await UpdateService.DownloadAsync(update, progress);

            UpdateText.Text = "Installing. USB Mon will restart when it's done.";
            UpdateService.LaunchInstaller(installer);
            ((App)Application.Current).ExitApplication();
        }
        catch (Exception ex)
        {
            ShowUpdateAvailable(update, "Update failed: " + ex.Message);
        }
        finally
        {
            _updateBusy = false;
            UpdateInstallButton.IsEnabled = true;
        }
    }

    private void UpdateNotes_Click(object sender, RoutedEventArgs e) =>
        UpdateService.OpenInBrowser(_pendingUpdate?.ReleaseUrl ?? UpdateService.RepoUrl + "/releases");

    private void UpdateDismiss_Click(object sender, RoutedEventArgs e) => UpdateBanner.Visibility = Visibility.Collapsed;
}
