#define MyAppName "School DTR System"
#define MyAppVersion "1.0.0-beta"
#define MyAppPublisher "School DTR"
#define MyAppExeName "SchoolDTR.exe"

[Setup]
AppId={{F9D51B34-9C0E-47E8-8E33-123456789ABC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SchoolDTR
DefaultGroupName={#MyAppName}
OutputDir=installer-output
OutputBaseFilename=SchoolDTR_Beta_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupLogging=yes
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create Desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Dirs]
Name: "{commonappdata}\SchoolDTR"; Permissions: users-modify
Name: "{commonappdata}\SchoolDTR\logs"; Permissions: users-modify
Name: "{commonappdata}\SchoolDTR\cache"; Permissions: users-modify
Name: "{commonappdata}\SchoolDTR\assets"; Permissions: users-modify

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

Source: "assets\default_logo.png"; DestDir: "{commonappdata}\SchoolDTR\assets"; DestName: "default_logo.png"; Flags: ignoreversion skipifsourcedoesntexist

Source: "installer-assets\zkteco\zkemkeeper.dll"; DestDir: "{app}\sdk"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
Source: "installer-assets\zkteco\commpro.dll"; DestDir: "{app}\sdk"; Flags: ignoreversion restartreplace skipifsourcedoesntexist
Source: "installer-assets\zkteco\plcommpro.dll"; DestDir: "{app}\sdk"; Flags: ignoreversion restartreplace skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{cmd}"; Parameters: "/C del /F /Q ""{commonappdata}\SchoolDTR\license_cache*.json"" 2>NUL"; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/C del /F /Q ""{commonappdata}\SchoolDTR\license_cache*.bin"" 2>NUL"; Flags: runhidden waituntilterminated

Filename: "{syswow64}\regsvr32.exe"; Parameters: "/s ""{app}\sdk\zkemkeeper.dll"""; StatusMsg: "Registering ZKTeco biometric SDK..."; Flags: waituntilterminated skipifdoesntexist

Filename: "{app}\{#MyAppExeName}"; Description: "Launch School DTR System now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{commonappdata}\SchoolDTR\license_cache*.json"
Type: files; Name: "{commonappdata}\SchoolDTR\license_cache*.bin"