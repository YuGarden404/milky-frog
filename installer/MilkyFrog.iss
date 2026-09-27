#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif

[Setup]
AppId={{7C4A8F15-1E75-4B7E-9E5C-9AA5B6F4D2D1}
AppName=Milky Frog
AppVersion={#MyAppVersion}
AppPublisher=YuGarden404
DefaultDirName={autopf}\Milky Frog
DefaultGroupName=Milky Frog
OutputDir=..\artifacts\installer
OutputBaseFilename=MilkyFrog-Setup-{#MyAppVersion}
SetupIconFile=..\src\MilkyFrog.App\Assets\Icons\milky-frog.ico
UninstallDisplayIcon={app}\MilkyFrog.exe
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Milky Frog"; Filename: "{app}\MilkyFrog.exe"
Name: "{autodesktop}\Milky Frog"; Filename: "{app}\MilkyFrog.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\MilkyFrog.exe"; Description: "Launch Milky Frog"; Flags: nowait postinstall skipifsilent
