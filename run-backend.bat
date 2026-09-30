@echo off
setlocal
cd /d "%~dp0backend\SWP301BETASK"
dotnet restore
if errorlevel 1 exit /b 1
dotnet run --launch-profile https
