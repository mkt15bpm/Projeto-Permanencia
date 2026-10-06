#define MyAppName "Projeto Permanência 15º BPM"
#define MyAppVersion "1.0.6"
#define MyAppExeName "PermanenciaWpf.exe"

[Setup]
AppId={{B1F7D57E-83A5-4EC7-B62B-8E518B4F3144}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\Permanencia15BPM
DefaultGroupName={#MyAppName}
OutputDir=..\outputs
OutputBaseFilename=Projeto-Permanencia-WPF-Instalador-1.0.6
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppName}
SetupIconFile=..\PermanenciaWpf\Assets\cpu-icon.ico

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Dirs]
Name: "C:\Permanencia15BPM\Dados"; Permissions: users-modify
Name: "C:\Permanencia15BPM\Backup"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\Relatório Diário – 15º BPM"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Relatório Diário – 15º BPM"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
