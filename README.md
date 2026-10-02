# USB Mon

A Windows system-tray utility that logs USB device activity in real time — arrivals, removals, and status/problem
code changes — including devices that fail to enumerate (the "Unknown USB Device (Device Descriptor Request Failed)"
case Device Manager gives you with no useful detail).

**Download:** the latest installer is on the [Releases](https://github.com/rosscarlson/USBMon/releases/latest) page.
Once installed, USB Mon checks for new versions on its own.

## Features

- **Runs in the tray**, continuously, whether or not the window is open. Closing the window hides it; Exit from the
  tray menu actually quits.
- **Low-level capture**: a hidden window registers for `WM_DEVICECHANGE` (all device interface classes) and also
  diffs snapshots of the device tree under the USB enumerator (SetupAPI/CfgMgr32) on every `DEVNODES_CHANGED`
  broadcast and on a periodic fallback poll — so a device that never registers a clean interface still shows up,
  with its status and problem code (e.g. Code 43).
- **Real-time event grid**: every column is sortable; double-click a row for every property that could be read,
  with copy-to-clipboard.
- **Per-launch log file**, tab-delimited, flushed on every write: `%LOCALAPPDATA%\USBMon\logs\`.
- **Dark** (default), Light, or System theme.
- **Auto-update** from GitHub Releases.
- Fully per-user: installs without admin rights, never writes to Program Files, "Run at startup" uses the per-user
  `HKCU` Run key.

## Build

Requires the .NET 8 SDK and Inno Setup 6.

```powershell
.\build.ps1              # -> artifacts\USBMonSetup-<version>.exe
```

## Releasing

1. Bump `<Version>` in `USBMon/USBMon.csproj` and commit.
2. Tag the commit and push the tag:
   ```powershell
   git tag v1.0.1
   git push origin v1.0.1
   ```
3. The `Release` workflow builds `USBMonSetup-<version>.exe` and publishes it as a GitHub Release.

## How auto-update works

- On startup, and when you click **Check for updates** in the tray menu, the app reads `releases/latest` from the
  GitHub API and compares the tag to its own version.
- If a newer release exists, a banner in the main window offers **Install update**. The app then:
  1. Downloads the `USBMonSetup-*.exe` asset to `%TEMP%\USBMon-Update`.
  2. Checks the download against the SHA-256 digest GitHub publishes for the asset.
  3. Runs the installer with `/SILENT` — no admin prompt, since USB Mon is a per-user install.
  4. Exits; the installer upgrades `%LOCALAPPDATA%\Programs\USBMon` in place (fixed `AppId`) and relaunches it.

## Layout

```
USBMon/
  Capture/      NativeMethods (SetupAPI/CfgMgr32/user32 P/Invoke), DeviceNotificationWindow (WM_DEVICECHANGE),
                DeviceSnapshot (enumerate + read device properties), DeviceWatcher (combines both + polling)
  Models/       DeviceInfo, DeviceRecord, DeviceEventType
  Filtering/    IEventFilter (seam for future filters; none implemented yet)
  Settings/     AppSettings, SettingsManager (%LOCALAPPDATA%\USBMon\settings.json), StartupManager (HKCU Run key)
  Logging/      EventLogger (per-launch tab-delimited log), ErrorLog (diagnostics)
  Updates/      UpdateService (GitHub Releases check, download, install)
  Theming/      ThemeManager (palette swap, system theme tracking, dark title bar)
  Themes/       Dark.xaml / Light.xaml palettes, Controls.xaml styles
installer/      Inno Setup script (per-user install, no admin required, fixed AppId)
tools/          make-icon.ps1 (regenerates Assets/USBMon.ico)
.github/        CI build + tag-triggered release workflow
```
