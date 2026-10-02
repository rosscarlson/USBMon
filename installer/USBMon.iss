; Inno Setup script for USB Mon.
; Built by build.ps1 (locally and by the GitHub release workflow), which passes AppVersion and PublishDir.
; The in-app updater downloads this installer from GitHub Releases and runs it with /SILENT.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

#define AppName "USB Mon"
#define AppExe "USBMon.exe"
#define RepoUrl "https://github.com/rosscarlson/USBMon"

[Setup]
; Never change AppId - upgrades and the auto-updater rely on it.
AppId={{A6C1E2F4-9B3D-4E7A-8C5F-1D2B3E4F5A6C}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=rosscarlson
AppPublisherURL={#RepoUrl}
AppSupportURL={#RepoUrl}/issues
AppUpdatesURL={#RepoUrl}/releases
VersionInfoVersion={#AppVersion}
; Per-user install: no admin rights required to install, run, or auto-update.
DefaultDirName={userpf}\USBMon
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
DisableDirPage=auto
OutputBaseFilename=USBMonSetup-{#AppVersion}
SetupIconFile=..\USBMon\Assets\USBMon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
; force: USB Mon may be running quietly in the tray
CloseApplications=force
RestartApplications=no

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; "Run at startup" is set by the app itself (HKCU Run key); remove it on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "USBMon"; Flags: uninsdeletevalue

[UninstallRun]
; Ask a running (tray) instance to exit cleanly so it closes its log file, then make sure it's gone.
Filename: "{app}\{#AppExe}"; Parameters: "--exit"; Flags: runhidden waituntilterminated; RunOnceId: "ExitUSBMon"
Filename: "{sys}\timeout.exe"; Parameters: "/t 2 /nobreak"; Flags: runhidden; RunOnceId: "WaitUSBMon"
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden; RunOnceId: "StopUSBMon"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"

[Run]
; Interactive install: optional "Launch" checkbox on the last page.
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
; Silent install (auto-update): relaunch the app.
Filename: "{app}\{#AppExe}"; Flags: nowait skipifnotsilent
