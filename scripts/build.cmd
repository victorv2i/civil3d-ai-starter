@echo off
rem Builds MyTools. Civil 3D picks up the new build by itself within a few seconds.
setlocal
set "DOTNET=dotnet"
if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" set "DOTNET=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
if exist "%~dp0..\build\use-system-dotnet" set "DOTNET=dotnet"
"%DOTNET%" build "%~dp0..\MyTools.sln" -nologo %*
exit /b %ERRORLEVEL%
