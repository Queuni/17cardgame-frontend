; -----------------------------------------------------------
; 17 Card Game - Professional Installer Script
; -----------------------------------------------------------

[Setup]
AppName=17 Card Game
AppVersion=1.0.0
AppPublisher=BVB Apps
AppPublisherURL=https://17cardgame.com
DefaultDirName={autopf}\17CardGame
DefaultGroupName=17 Card Game
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=17CardGameInstaller
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile="..\Assets\Resources\images\favicon.ico"
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin

[Files]
; Copy only necessary build files (not .pdb, not temp files)
Source: "Windows\17 Card Game.exe"; DestDir: "{app}"
Source: "Windows\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "..\Assets\Resources\images\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\17 Card Game"; Filename: "{app}\17 Card Game.exe"; IconFilename: "{app}\favicon.ico"
Name: "{commondesktop}\17 Card Game"; Filename: "{app}\17 Card Game.exe"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a Desktop Shortcut"; GroupDescription: "Additional Tasks:"; Flags: unchecked

[Run]
Filename: "{app}\17 Card Game.exe"; Description: "Launch 17 Card Game"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
