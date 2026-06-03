@echo off
title School DTR License Signer
cd /d "%~dp0"

echo =====================================
echo   SCHOOL DTR LICENSE SIGNER
echo =====================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sign-license.ps1"

echo.
echo Finished with exit code %ERRORLEVEL%
pause