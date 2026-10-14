@echo off
rem Quita Lacteos Madeline y sus accesos directos. Conserva los datos del negocio.
setlocal
set "SCRIPT=%~dp0scripts\instalar.ps1"
if not exist "%SCRIPT%" set "SCRIPT=%~dp0instalar.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -Desinstalar
set "CODIGO=%ERRORLEVEL%"
echo.
pause
exit /b %CODIGO%
