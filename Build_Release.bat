@echo off
title Banga Photobooth - Build & Package
color 0B

echo ==================================================
echo      BANGA PHOTOBOOTH - RELEASE PACKAGER
echo ==================================================
echo.

set "DIST_DIR=%~dp0Dist\Banga Photobooth"
set "PROJECT_DIR=%~dp0"

:: 1. Clean previous build
echo [1/5] Cleaning previous builds...
if exist "%DIST_DIR%" rd /s /q "%DIST_DIR%"
dotnet clean --configuration Release

:: 2. Build and Publish
echo.
echo [2/5] Building Release Version...
echo --------------------------------------------------
:: Using --no-self-contained to rely on installed runtime (smaller size) 
:: or use --self-contained true to bundle runtime (larger size, no install needed).
:: For now, we use standard build output to keep it simple with the installer instructions.
dotnet publish -c Release -r win-x64 --self-contained false -o "%DIST_DIR%"

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed!
    pause
    exit /b
)

:: 3. Copy Drivers
echo.
echo [3/5] Copying Drivers...
echo --------------------------------------------------
xcopy "%PROJECT_DIR%Drivers" "%DIST_DIR%\Drivers" /E /I /Y
if %ERRORLEVEL% NEQ 0 echo [WARNING] Drivers folder copy failed or empty.

:: 4. Copy Install Scripts & Guide
echo.
echo [4/5] Copying Installer & Guides...
echo --------------------------------------------------
copy "%PROJECT_DIR%Install_Banga.bat" "%DIST_DIR%\"
copy "%PROJECT_DIR%SETUP_GUIDE.md" "%DIST_DIR%\"
:: Copy config if it exists
if exist "%PROJECT_DIR%config.json" copy "%PROJECT_DIR%config.json" "%DIST_DIR%\"

:: 5. Finalize
echo.
echo [5/5] Packaging Complete!
echo --------------------------------------------------
echo.
echo Your release package is ready at:
echo %DIST_DIR%
echo.
echo You can now zip the 'Dist\Banga Photobooth' folder for distribution.
echo.
pause

