@echo off
cd /d "%~dp0backend\LegacyVault"
dotnet run --launch-profile https
pause
