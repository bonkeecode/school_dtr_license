#define MyAppName "School DTR System"
#define MyAppVersion "1.0.0-beta"
#define MyAppExeName "SchoolDTR.exe"

[Setup]
AppId={{F9D51B34-9C0E-47E8-8E33-123456789ABC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=School DTR
DefaultDirName={commonappdata}\SchoolDTR
DefaultGroupName={#MyAppName}
OutputDir=installer-output
OutputBaseFilename=SchoolDTR_Beta_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x86 x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupLogging=yes
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create Desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Dirs]
Name: "{app}"; Permissions: users-full
Name: "{app}\logs"; Permissions: users-full
Name: "{app}\cache"; Permissions: users-full
Name: "{app}\sdk"; Permissions: users-full
Name: "{app}\tools"; Permissions: users-full

[Files]

; Main application (self-contained)
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; ZKTeco SDK
Source: "installer-assets\zkteco\zkemkeeper.dll"; DestDir: "{app}\sdk"; Flags: ignoreversion restartreplace
Source: "installer-assets\zkteco\commpro.dll"; DestDir: "{app}\sdk"; Flags: ignoreversion restartreplace
Source: "installer-assets\zkteco\plcommpro.dll"; DestDir: "{app}\sdk"; Flags: ignoreversion restartreplace

; Optional tools
Source: "tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]

; remove stale license files
Filename: "{cmd}"; Parameters: "/C del /F /Q ""{app}\license_cache.json"" 2>NUL"; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/C del /F /Q ""{app}\cache\license_cache.json"" 2>NUL"; Flags: runhidden waituntilterminated

; ensure permissions
Filename: "icacls"; Parameters: """{app}"" /grant Users:(OI)(CI)F /T /C"; Flags: runhidden waituntilterminated

; launch app
Filename: "{app}\{#MyAppExeName}"; Description: "Launch School DTR System now"; Flags: nowait postinstall skipifsilent

; Register zkemkeeper.dll using 32-bit regsvr32
Filename: "{syswow64}\regsvr32.exe"; \
Parameters: "/s ""{app}\sdk\zkemkeeper.dll"""; \
StatusMsg: "Registering ZKTeco biometric SDK..."; \
Flags: waituntilterminated
[UninstallDelete]
Type: files; Name: "{app}\license_cache.json"
Type: filesandordirs; Name: "{app}\cache"
Type: filesandordirs; Name: "{app}\logs"