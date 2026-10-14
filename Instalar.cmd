@echo off
rem Instala o actualiza Lacteos Madeline para el usuario actual.
rem Funciona desde el repositorio (compila) o desde el paquete .zip (ya compilado).
setlocal
set "SCRIPT=%~dp0scripts\instalar.ps1"
if not exist "%SCRIPT%" set "SCRIPT=%~dp0instalar.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
set "CODIGO=%ERRORLEVEL%"
echo.
pause
exit /b %CODIGO%
