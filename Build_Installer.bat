@echo off
echo ==================================================
echo   BANGA PHOTOBOOTH - INSTALLER BUILDER
echo ==================================================
echo.

:: Step 1: Build Release
echo [1/2] Building Release...
echo --------------------------------------------------
call Build_Release.bat

:: Step 2: Check if Inno Setup is installed
echo.
echo [2/2] Creating Installer...
echo --------------------------------------------------

:: Common Inno Setup installation paths
set ISCC_PATH=

if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    set ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
) else if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
    set ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe
) else if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" (
    set ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe
)

if "%ISCC_PATH%"=="" (
    echo.
    echo ERROR: Inno Setup 6 not found!
    echo.
    echo Please install Inno Setup 6 from:
    echo https://jrsoftware.org/isdl.php
    echo.
    echo After installation, run this script again.
    echo.
    pause
    exit /b 1
)

echo Found Inno Setup at: %ISCC_PATH%
echo.

:: Run Inno Setup Compiler
"%ISCC_PATH%" "Installer\BangaPhotobooth.iss"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Installer build failed!
    pause
    exit /b 1
)

echo.
echo ==================================================
echo   INSTALLER BUILD COMPLETE!
echo ==================================================
echo.
echo Your installer is ready at:
echo Installer\Output\BangaPhotobooth_Setup_v1.0.0.exe
echo.
pause

