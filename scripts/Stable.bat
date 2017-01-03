@ECHO off
TITLE Downloading NadekoBot, please wait
::Setting convenient to read variables which don't delete the windows temp folder
SET root=%~dp0
CD /D %root%
SET rootdir=%cd%
SET build1=%root%NadekoInstall_Temp\NadekoBot\Discord.Net\src\Discord.Net.Core\
SET build2=%root%NadekoInstall_Temp\NadekoBot\Discord.Net\src\Discord.Net.Rest\
SET build3=%root%NadekoInstall_Temp\NadekoBot\Discord.Net\src\Discord.Net.WebSocket\
SET build4=%root%NadekoInstall_Temp\NadekoBot\Discord.Net\src\Discord.Net.Commands\
SET build5=%root%NadekoInstall_Temp\NadekoBot\src\NadekoBot\
SET installtemp=%root%NadekoInstall_Temp\
::Deleting traces of last setup for the sake of clean folders, if by some miracle it still exists
IF EXIST %installtemp% ( RMDIR %installtemp% /S /Q >nul 2>&1)
::Checks that both git and dotnet are installed
dotnet --version >nul 2>&1 || GOTO :dotnet
git --version >nul 2>&1 || GOTO :git
::Creates the install directory to work in and get the current directory because spaces ruins everything otherwise
:start
MKDIR NadekoInstall_Temp
CD /D %installtemp%
::Downloads the latest version of Nadeko
ECHO Downloading Nadeko...
ECHO.
git clone -b master --recursive --depth 1 --progress https://github.com/Kwoth/NadekoBot.git >nul
IF %ERRORLEVEL% EQU 128 (GOTO :giterror)
TITLE Installing NadekoBot, please wait
ECHO.
ECHO Installing...
::Building Nadeko
CD /D %build1%
dotnet restore >nul 2>&1
CD /D %build2%
dotnet restore >nul 2>&1
CD /D %build3%
dotnet restore >nul 2>&1
CD /D %build4%
dotnet restore >nul 2>&1
CD /D %build5%
dotnet restore >nul 2>&1
dotnet build --configuration Release >nul 2>&1
::Attempts to backup old files if they currently exist in the same folder as the batch file
IF EXIST "%root%NadekoBot\" (GOTO :backupinstall)
:freshinstall
	::Moves the NadekoBot folder to keep things tidy
	ROBOCOPY "%root%NadekoInstall_Temp" "%rootdir%" /E /MOVE >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	GOTO :end
:backupinstall
	TITLE Backing up old files
	ECHO.
	ECHO Make sure to close any files such as NadekoBot.db before PRESSing ANY KEY TO CONTINUE to prevent data loss
	PAUSE >nul 2>&1
	::Recursively copies all files and folders from NadekoBot to NadekoBot_Old
	ROBOCOPY "%root%NadekoBot" "%root%NadekoBot_Old" /MIR >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO Old files backed up to NadekoBot_Old
	::Copies the credentials and database from the backed up data to the new folder
	COPY "%root%NadekoBot_Old\src\NadekoBot\credentials.json" "%installtemp%NadekoBot\src\NadekoBot\credentials.json" >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO credentials.json copied to new folder
	ROBOCOPY "%root%NadekoBot_Old\src\NadekoBot\bin" "%installtemp%NadekoBot\src\NadekoBot\bin" /E >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO Old bin folder copied to new folder
	ROBOCOPY "%root%NadekoBot_Old\src\NadekoBot\data" "%installtemp%NadekoBot\src\NadekoBot\data" /E >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO Old data folder copied to new folder
	::Moves the setup Nadeko folder
	RMDIR "%root%NadekoBot\" /S /Q >nul 2>&1
	ROBOCOPY "%root%NadekoInstall_Temp" "%rootdir%" /E /MOVE >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	GOTO :end
:dotnet
	::Terminates the batch script if it can't run dotnet --version
	TITLE Error!
	ECHO dotnet not found, make sure it's been installed as per the guides instructions!
	ECHO Press any key to exit.
	PAUSE >nul 2>&1
	CD /D "%root%"
	GOTO :EOF
:git
	::Terminates the batch script if it can't run git --version
	TITLE Error!
	ECHO git not found, make sure it's been installed as per the guides instructions!
	ECHO Press any key to exit.
	PAUSE >nul 2>&1
	CD /D "%root%"
	GOTO :EOF
:giterror
	ECHO.
	ECHO Git clone failed, trying again
	RMDIR %installtemp% /S /Q >nul 2>&1
	GOTO :start
:copyerror
	::If at any point a copy error is encountered 
	TITLE Error!
	ECHO.
	ECHO An error in copying data has been encountered, returning an exit code of %ERRORLEVEL%
	ECHO.
	ECHO Make sure to close any files, such as `NadekoBot.db` before continuing or try running the installer as an Administrator
	PAUSE >nul 2>&1
	CD /D "%root%"
	GOTO :EOF
:end
	::Normal execution of end of script
	TITLE Installation complete!
	CD /D "%root%"
	RMDIR /S /Q "%installtemp%" >nul 2>&1
	ECHO.
	ECHO Installation complete, press any key to close this window!
	PAUSE >nul 2>&1
	del Stable.bat
