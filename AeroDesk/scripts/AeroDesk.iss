#define AppName "AeroDesk"
#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\publish"
#endif

[Setup]
AppId={{D85751CE-A5C5-493E-BCE4-BE3DE7A5C56F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=AeroDesk Project
DefaultDirName={localappdata}\Programs\AeroDesk
DefaultGroupName=AeroDesk
UninstallDisplayIcon={app}\AeroDesk.exe
OutputDir=..\artifacts\installer
OutputBaseFilename=AeroDesk-Setup-{#AppVersion}-x64
Compression=lzma2/fast
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
WizardStyle=modern
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
CloseApplications=force
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startup"; Description: "Start AeroDesk when I sign in"; GroupDescription: "Windows startup:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\AeroDesk.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\AeroDesk.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourceDir}\*.json"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\AeroDesk"; Filename: "{app}\AeroDesk.exe"
Name: "{autodesktop}\AeroDesk"; Filename: "{app}\AeroDesk.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AeroDesk"; ValueData: """{app}\AeroDesk.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\AeroDesk.exe"; Description: "Launch AeroDesk"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AeroDesk');
    if MsgBox('Keep your AeroDesk settings and notes for a future reinstall?', mbConfirmation, MB_YESNO) = IDNO then
      DelTree(ExpandConstant('{userappdata}\AeroDesk'), True, True, True);
  end;
end;
