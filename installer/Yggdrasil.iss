#define AppName "Yggdrasil"
#define AppPublisher "xande"

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#ifndef RuntimeIdentifier
  #define RuntimeIdentifier "win-x64"
#endif

#ifndef OutputBaseFilename
  #define OutputBaseFilename AppName + "-setup-" + RuntimeIdentifier + "-" + AppVersion
#endif

#ifndef SourceDir
  #error SourceDir define is required.
#endif

#ifndef OutputDir
  #define OutputDir AddBackslash(SourcePath) + "..\artifacts\release"
#endif

#ifndef RepoRoot
  #define RepoRoot AddBackslash(SourcePath) + ".."
#endif

[Setup]
AppId={{F6B46C7E-6D6B-4B14-812A-B524F2D21753}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile={#RepoRoot}\Yggdrasil.Presentation\Assets\Yggdrasil.ico
UninstallDisplayIcon={app}\Yggdrasil.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\Yggdrasil.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Yggdrasil.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Yggdrasil.exe"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
