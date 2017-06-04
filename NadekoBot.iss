#define sysfolder "system"
#define version "1.41"
#define target "win7-x64"

[Setup]
AppName=NadekoBot
AppVersion={#version}
AppPublisher=Kwoth
DefaultDirName={pf}\NadekoBot
DefaultGroupName=NadekoBot
UninstallDisplayIcon={app}\{#sysfolder}\NadekoBot.exe
Compression=lzma2
SolidCompression=yes
OutputDir=userdocs:projekti/NadekoInstallerOutput
OutputBaseFilename=NadekoBot-setup-{#version}
AppReadmeFile=http://nadekobot.readthedocs.io/en/latest/Commands%20List/

[Files]
;install 
Source: "src\NadekoBot\bin\Release\PublishOutput\{#target}\*"; DestDir: "{app}\{#sysfolder}"; Permissions: users-modify; Flags: recursesubdirs onlyifdoesntexist ignoreversion createallsubdirs; Excludes: "*.pdb, *.db"

;reinstall - i want to copy all files, but i don't want to overwrite any data files because users will lose their customization if they don't have a backup, 
;            and i don't want them to have to backup and then copy-merge into data folder themselves, or lose their currency images due to overwrite.
Source: "src\NadekoBot\bin\Release\PublishOutput\{#target}\*"; DestDir: "{app}\{#sysfolder}"; Permissions: users-modify; Flags: recursesubdirs onlyifdestfileexists createallsubdirs; Excludes: "*.pdb, *.db, data\*, credentials.json";
Source: "src\NadekoBot\bin\Release\PublishOutput\{#target}\data\*"; DestDir: "{app}\{#sysfolder}\data"; Permissions: users-modify; Flags: recursesubdirs onlyifdoesntexist createallsubdirs;

;readme   
;Source: "readme"; DestDir: "{app}"; Flags: isreadme

[Run]
Filename: "http://nadekobot.readthedocs.io/en/latest/Commands%20List/"; Flags: postinstall checked shellexec runasoriginaluser; Description: "Open Command List"

[Icons]
; for pretty install directory
Name: "{app}\NadekoBot"; Filename: "{app}\{#sysfolder}\NadekoBot.exe"; IconFilename: "{app}\{#sysfolder}\nadeko_icon.ico"
Name: "{app}\credentials"; Filename: "{app}\{#sysfolder}\credentials.json" 
Name: "{app}\data"; Filename: "{app}\{#sysfolder}\data" 
; desktop shortcut 
Name: "{commondesktop}\NadekoBot"; Filename: "{app}\NadekoBot";