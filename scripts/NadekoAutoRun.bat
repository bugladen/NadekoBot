@ECHO off
@TITLE NadekoBot
:auto
CD /D %~dp0NadekoBot\src\NadekoBot
dotnet run --configuration Release
goto auto
