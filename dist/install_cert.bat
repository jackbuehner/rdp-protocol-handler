@echo off

rem Relaunch the script with elevated privileges if not already running as admin
if not "%1"=="elevated" (
    echo Requesting administrative privileges...
    powershell -Command "Start-Process '%~f0' -ArgumentList 'elevated' -Verb RunAs"
    exit /b
)

set certName=RDP Protocol Handler.cer
set certPath=%~dp0%certName%

if exist "%certPath%" (
    certutil -addstore "root" "%certPath%"
    if %errorlevel% equ 0 (
        echo Certificate installed successfully.
    ) else (
        echo Failed to install certificate. Error code: %errorlevel%
    )
) else (
    echo Certificate file not found at: %certPath%
)

pause
