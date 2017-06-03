; -- Example1.iss --
; Demonstrates copying 3 files and creating an icon.

; SEE THE DOCUMENTATION FOR DETAILS ON CREATING .ISS SCRIPT FILES!
#define sysfolder "system"


[Setup]
AppName=NadekoBot
AppVersion=1.41
DefaultDirName={pf}\NadekoBot
DefaultGroupName=NadekoBot
UninstallDisplayIcon={app}\NadekoBot.exe
Compression=lzma2
SolidCompression=yes
OutputDir=userdocs:Inno Setup Examples Output

[Files]
Source: "bin\Release\PublishOutput\win7-x64\*"; DestDir: "{app}\system"; Flags: recursesubdirs
Source: "..\..\docs\Readme.md"; DestDir: "{app}"; Flags: isreadme

[Icons]
; for pretty install directory
Name: "{app}\NadekoBot"; Filename: "{app}\{#sysfolder}\NadekoBot.exe"; IconFilename: "{app}\{#sysfolder}\nadeko_icon.ico"
Name: "{app}\credentials"; Filename: "{app}\{#sysfolder}\credentials.json" 
Name: "{app}\data"; Filename: "{app}\{#sysfolder}\data" 
; desktop shortcut 
Name: "{commondesktop}\NadekoBot"; Filename: "{app}\NadekoBot";
