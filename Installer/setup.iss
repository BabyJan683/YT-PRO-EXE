; ═══════════════════════════════════════════════════════════════════════════════
; YT Downloader Pro — Professional Inno Setup Script
; Requires Inno Setup 6.x: https://jrsoftware.org/isdl.php
; ═══════════════════════════════════════════════════════════════════════════════

#define AppName        "YT Downloader Pro"
#define AppVersion     "2.0.0"
#define AppPublisher   "YT Downloader Pro"
#define AppURL         "https://ytdownloaderpro.app"
#define AppExeName     "YTDownloaderPro.exe"
#define AppDescription "Professional YouTube & Video Downloader"
#define AppCopyright   "Copyright © 2026 YT Downloader Pro"
#define AppID          "{A3F8B2C1-4D6E-8F0A-1B2C-3D4E5F6A7B8C}"

; ── Setup metadata ────────────────────────────────────────────────────────────
[Setup]
AppId={#AppID}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} v{#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/support
AppUpdatesURL={#AppURL}/updates
AppCopyright={#AppCopyright}

; Install location — user can change on wizard page
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DirExistsWarning=no
DisableDirPage=no

; Program group in Start Menu — user can change
DisableProgramGroupPage=no
AllowNoIcons=yes

; Output
OutputDir=..\..\Output
OutputBaseFilename=YTDownloaderPro_v{#AppVersion}_Setup
SetupIconFile=..\Resources\app.ico

; Wizard appearance
WizardStyle=modern
WizardSizePercent=120
WizardResizable=no

; Compression — maximum
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMADictionarySize=1048576

; Privileges — user-level by default, can elevate
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Uninstaller
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} v{#AppVersion}
CreateUninstallRegKey=yes

; Version info embedded into installer EXE
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppDescription}
VersionInfoCopyright={#AppCopyright}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

; Windows 10+ required for .NET 8 WPF
MinVersion=10.0.17763
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

; Installer splash / header images (optional — place in Installer\ folder)
; WizardImageFile=WizardImage.bmp
; WizardSmallImageFile=WizardSmallImage.bmp

; ── Languages ─────────────────────────────────────────────────────────────────
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── Custom messages ───────────────────────────────────────────────────────────
[CustomMessages]
english.InstallModeTitle=Installation Mode
english.InstallModeSubtitle=Choose where to store your settings
english.InstallModeDesc=Select the installation mode:
english.StandardMode=Standard installation (recommended)
english.StandardModeDesc=Settings stored in %AppData%\YTDownloaderPro
english.PortableMode=Portable installation
english.PortableModeDesc=All data stored inside the install folder (great for USB drives)
english.StartupTitle=Windows Startup
english.StartupSubtitle=Launch option
english.DotNetMissing=.NET 8 Runtime is required but not installed.%nDo you want to download it now?

; ── Install tasks (checkboxes on wizard) ──────────────────────────────────────
[Tasks]
; Desktop shortcut — off by default so user opts in
Name: "desktopicon";    Description: "Create a &desktop shortcut";             GroupDescription: "Shortcuts:";          Flags: unchecked
; Quick-launch bar (XP/Vista only, hidden on Win7+)
Name: "quicklaunch";    Description: "Add to Quick Launch bar";               GroupDescription: "Shortcuts:";          Flags: unchecked; OnlyBelowVersion: 6.1

; ── Windows Startup task  (user sees it on wizard) ────────────────────────────
Name: "startup";        Description: "Launch {#AppName} when Windows starts"; GroupDescription: "Windows Startup:";    Flags: unchecked

; ── System tray ───────────────────────────────────────────────────────────────
Name: "trayalways";     Description: "Always show icon in system tray";       GroupDescription: "System Tray:";        Flags: checkedonce

; ── Browser extension helper ──────────────────────────────────────────────────
Name: "browserext";     Description: "Install browser extension helper";      GroupDescription: "Browser Integration:"

; ── Files to install ──────────────────────────────────────────────────────────
[Files]
; Main application EXE  (single-file publish)
Source: "..\bin\Release\net8.0-windows\win-x64\publish\{#AppExeName}"; \
        DestDir: "{app}"; Flags: ignoreversion

; Resources: yt-dlp, aria2, ffmpeg (skip if not present — app downloads on first run)
Source: "..\Resources\yt-dlp.exe";   DestDir: "{app}\Resources"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Resources\aria2c.exe";   DestDir: "{app}\Resources"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Resources\ffmpeg.exe";   DestDir: "{app}\Resources"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Resources\ffprobe.exe";  DestDir: "{app}\Resources"; Flags: ignoreversion skipifsourcedoesntexist

; App icon (for uninstaller display + shortcuts)
Source: "..\Resources\app.ico";      DestDir: "{app}\Resources"; Flags: ignoreversion skipifsourcedoesntexist

; Browser extension
Source: "..\Browser\*";              DestDir: "{app}\Browser"; Flags: recursesubdirs ignoreversion; Tasks: browserext

; README
Source: "..\README.md";              DestDir: "{app}"; Flags: ignoreversion isreadme skipifsourcedoesntexist

; ── Start Menu & Desktop icons ────────────────────────────────────────────────
[Icons]
; Start Menu group
Name: "{group}\{#AppName}";                               Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Resources\app.ico"
Name: "{group}\{cm:UninstallProgram,{#AppName}}";         Filename: "{uninstallexe}"

; Desktop shortcut (opt-in task)
Name: "{autodesktop}\{#AppName}";                         Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Resources\app.ico"; Tasks: desktopicon

; Quick Launch (legacy)
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; \
      Filename: "{app}\{#AppExeName}"; Tasks: quicklaunch

; ── Registry entries ──────────────────────────────────────────────────────────
[Registry]
; ── Windows Startup (registered only when user ticked the task) ───────────────
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\{#AppExeName}"" --minimized"; \
  Flags: uninsdeletevalue; Tasks: startup

; ── App registration ──────────────────────────────────────────────────────────
Root: HKCU; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
  ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; \
  Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
  ValueType: string; ValueName: "Version";     ValueData: "{#AppVersion}"; \
  Flags: uninsdeletekey

; ── .ytdlp file association ───────────────────────────────────────────────────
Root: HKCU; Subkey: "SOFTWARE\Classes\.ytdlp";                                     ValueType: string; ValueName: ""; ValueData: "YTDownloaderProJob"; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Classes\YTDownloaderProJob";                         ValueType: string; ValueName: ""; ValueData: "YT Downloader Pro Job File"; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Classes\YTDownloaderProJob\DefaultIcon";             ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Classes\YTDownloaderProJob\shell\open\command";      ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey

; ── Post-install: launch the app ──────────────────────────────────────────────
[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "Launch {#AppName}"; \
  Flags: nowait postinstall skipifsilent; \
  WorkingDir: "{app}"

; ── Uninstall cleanup ─────────────────────────────────────────────────────────
[UninstallDelete]
Type: filesandordirs; Name: "{app}\Resources"
Type: filesandordirs; Name: "{app}\Browser"
Type: dirifempty;     Name: "{app}"

; ── Pascal script — custom wizard pages ──────────────────────────────────────
[Code]

var
  InstallModePage : TInputOptionWizardPage;
  StartupPage     : TInputOptionWizardPage;
  g_Portable      : Boolean;

{ ── Wizard init: add custom pages ─────────────────────────────────────────── }
procedure InitializeWizard;
begin
  { Page: Installation Mode (Standard vs Portable) }
  InstallModePage := CreateInputOptionPage(
    wpSelectDir,
    ExpandConstant('{cm:InstallModeTitle}'),
    ExpandConstant('{cm:InstallModeSubtitle}'),
    ExpandConstant('{cm:InstallModeDesc}'),
    True, False);
  InstallModePage.Add(ExpandConstant('{cm:StandardMode} — {cm:StandardModeDesc}'));
  InstallModePage.Add(ExpandConstant('{cm:PortableMode} — {cm:PortableModeDesc}'));
  InstallModePage.Values[0] := True;
end;

{ ── Check .NET 8 runtime ───────────────────────────────────────────────────── }
function IsDotNetInstalled: Boolean;
var
  SubKey : String;
  Value  : Cardinal;
begin
  SubKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sdk';
  Result := RegQueryDWordValue(HKCU, SubKey, 'Version', Value);
  if not Result then
    Result := RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost');
end;

{ ── Ask to download .NET if missing ────────────────────────────────────────── }
function InitializeSetup: Boolean;
begin
  Result := True;
  if not IsDotNetInstalled then
  begin
    if MsgBox(ExpandConstant('{cm:DotNetMissing}'),
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
        'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.0-windows-x64-installer',
        '', '', SW_SHOWNORMAL, ewNoWait, 0);
      MsgBox('Please install .NET 8 Desktop Runtime, then re-run this installer.',
             mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;

{ ── After all files are installed ──────────────────────────────────────────── }
procedure CurStepChanged(CurStep: TSetupStep);
var
  PortableFlag : String;
begin
  if CurStep = ssPostInstall then
  begin
    g_Portable := InstallModePage.Values[1];
    if g_Portable then
    begin
      PortableFlag := ExpandConstant('{app}\portable.txt');
      SaveStringToFile(PortableFlag,
        'Portable mode — all data is stored in this folder. Delete this file to switch to AppData mode.',
        False);
    end;
  end;
end;

{ ── Validate before moving to next page ────────────────────────────────────── }
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
end;

{ ── Page title override (nicer heading on the directory page) ──────────────── }
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
end;
