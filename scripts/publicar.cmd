@echo off
rem Genera dist\LacteosMadeline-<version>-win-x64.zip para instalar en otras PCs.
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publicar.ps1" %*
set "CODIGO=%ERRORLEVEL%"
echo.
pause
exit /b %CODIGO%
