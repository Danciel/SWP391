@echo off
setlocal
cd /d "%~dp0frontend"
if not exist .env copy .env.example .env >nul
call npm install
if errorlevel 1 exit /b 1
call npm run dev
